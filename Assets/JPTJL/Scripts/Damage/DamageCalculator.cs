using UnityEngine;
using JPTJL.Combat;
using JPTJL.Data;

namespace JPTJL.Damage
{
    /// <summary>
    /// Pure deterministic damage calculation engine.
    /// Combines offensive scaling, QTE multipliers, defense mitigation, and Parry/Dodge reactions.
    /// </summary>
    public static class DamageCalculator
    {
        public static DamageResult Calculate(
            CharacterStatsData attacker,
            CharacterStatsData defender,
            SkillData skill,
            TimingResult attackTiming,
            DefenseType defenseAction = DefenseType.None,
            TimingResult defenseTiming = TimingResult.Miss,
            bool? forceCritical = null)
        {
            if (attacker == null || defender == null || skill == null)
            {
                return default;
            }

            // 1. Raw offensive power
            float rawDamage = skill.BasePower + (attacker.BaseAttack * skill.AttackScaling);

            // 2. QTE Attack multiplier
            float timingMultiplier = skill.GetTimingMultiplier(attackTiming);
            float damageAfterTiming = rawDamage * timingMultiplier;

            // 3. Critical strike evaluation
            bool isCrit = forceCritical ?? (Random.value < attacker.CriticalChance);
            if (isCrit)
            {
                damageAfterTiming *= attacker.CriticalMultiplier;
            }

            // 4. Armor / Defense mitigation curve: Damage * (100 / (100 + Defense))
            float defenseMitigationFactor = 100f / (100f + Mathf.Max(0, defender.BaseDefense));
            float postArmorDamage = damageAfterTiming * defenseMitigationFactor;

            // 5. Active defense reaction (Parry / Dodge)
            bool wasParried = false;
            bool wasDodged = false;
            int counterDamage = 0;
            float defenseMitigation = 0f;

            if (defenseAction == DefenseType.Parry)
            {
                switch (defenseTiming)
                {
                    case TimingResult.Perfect:
                        // Perfect Parry: 100% damage nullified + counter attack damage reflected to attacker
                        defenseMitigation = 1.0f;
                        wasParried = true;
                        counterDamage = Mathf.RoundToInt(postArmorDamage * 0.5f);
                        break;
                    case TimingResult.Good:
                        // Good Parry: 60% damage mitigation
                        defenseMitigation = 0.60f;
                        wasParried = true;
                        break;
                    case TimingResult.Miss:
                    default:
                        defenseMitigation = 0f;
                        break;
                }
            }
            else if (defenseAction == DefenseType.Dodge)
            {
                switch (defenseTiming)
                {
                    case TimingResult.Perfect:
                        // Perfect Dodge: 100% damage evaded
                        defenseMitigation = 1.0f;
                        wasDodged = true;
                        break;
                    case TimingResult.Good:
                        // Good Dodge: 75% damage evaded
                        defenseMitigation = 0.75f;
                        wasDodged = true;
                        break;
                    case TimingResult.Miss:
                    default:
                        defenseMitigation = 0f;
                        break;
                }
            }

            float finalDamageCalculated = postArmorDamage * (1f - defenseMitigation);
            int finalDamage = Mathf.Max(0, Mathf.RoundToInt(finalDamageCalculated));

            return new DamageResult(
                rawDamage: Mathf.RoundToInt(rawDamage),
                finalDamage: finalDamage,
                isCritical: isCrit,
                attackTiming: attackTiming,
                defenseAction: defenseAction,
                defenseTiming: defenseTiming,
                wasParried: wasParried,
                wasDodged: wasDodged,
                counterDamage: counterDamage
            );
        }
    }
}
