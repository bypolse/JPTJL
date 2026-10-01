using UnityEngine;
using JPTJL.Damage;
using JPTJL.FSM;

namespace JPTJL.Combat.States
{
    /// <summary>
    /// Calculates and applies damage formulas, counter-attacks, and checks victory/defeat conditions.
    /// </summary>
    public class DamageResolutionState : IState
    {
        private readonly CombatController controller;
        private float delayTimer;
        private const float ResolutionDelay = 0.6f;

        public DamageResolutionState(CombatController controller)
        {
            this.controller = controller;
        }

        public void Enter()
        {
            delayTimer = 0f;
            CombatEvents.TriggerPhaseChanged(CombatPhase.DamageResolution);

            CombatParticipant attacker = controller.IsPlayerTurn ? controller.Player : controller.Enemy;
            CombatParticipant defender = controller.IsPlayerTurn ? controller.Enemy : controller.Player;

            DefenseType defenseType = controller.IsPlayerTurn ? DefenseType.None : controller.ActiveDefenseType;
            TimingResult defenseTiming = controller.IsPlayerTurn ? TimingResult.Miss : controller.DefensiveTimingResult;

            DamageResult damageResult;

            if (controller.IsPlayerTurn && controller.LastQTEResults != null && controller.LastQTEResults.Count > 0)
            {
                // Multi-hit / single-hit resolution from Key QTE Engine
                int totalRaw = 0;
                float totalPostMultiplier = 0f;
                int baseHitPower = controller.CurrentSkill != null ? controller.CurrentSkill.BasePower : 10;
                float scaling = controller.CurrentSkill != null ? controller.CurrentSkill.AttackScaling : 0f;

                for (int i = 0; i < controller.LastQTEResults.Count; i++)
                {
                    var hit = controller.LastQTEResults[i];
                    float hitRaw = baseHitPower + (attacker.StatsData.BaseAttack * scaling);
                    float hitModified = hitRaw * hit.DamageMultiplier;

                    totalRaw += Mathf.RoundToInt(hitRaw);
                    totalPostMultiplier += hitModified;
                }

                // Armor mitigation
                float armorFactor = 100f / (100f + Mathf.Max(0, defender.StatsData.BaseDefense));
                int finalDamage = Mathf.Max(controller.LastQTEResults.Count, Mathf.RoundToInt(totalPostMultiplier * armorFactor));

                damageResult = new DamageResult(
                    rawDamage: totalRaw,
                    finalDamage: finalDamage,
                    isCritical: false,
                    attackTiming: controller.OffensiveTimingResult,
                    defenseAction: DefenseType.None,
                    defenseTiming: TimingResult.Miss,
                    wasParried: false,
                    wasDodged: false,
                    counterDamage: 0
                );
            }
            else
            {
                damageResult = DamageCalculator.Calculate(
                    attacker: attacker.StatsData,
                    defender: defender.StatsData,
                    skill: controller.CurrentSkill,
                    attackTiming: controller.OffensiveTimingResult,
                    defenseAction: defenseType,
                    defenseTiming: defenseTiming
                );
            }

            // Apply direct damage to defender (Shield absorbs first before HP inside CombatParticipant.TakeDamage)
            defender.TakeDamage(damageResult.FinalDamage);

            // Apply counter damage to attacker if a perfect parry was achieved
            if (damageResult.CounterDamage > 0)
            {
                attacker.TakeDamage(damageResult.CounterDamage);
            }

            CombatEvents.TriggerDamageResolved(attacker, defender, damageResult);
        }

        public void Update(float deltaTime)
        {
            delayTimer += deltaTime;
            if (delayTimer < ResolutionDelay) return;

            // Check if battle should end
            if (!controller.Player.IsAlive)
            {
                controller.BattleWon = false;
                controller.StateMachine.ChangeState(controller.BattleEndState);
                return;
            }

            if (!controller.Enemy.IsAlive)
            {
                controller.BattleWon = true;
                controller.StateMachine.ChangeState(controller.BattleEndState);
                return;
            }

            // Otherwise, alternate turn
            if (controller.IsPlayerTurn)
            {
                controller.StateMachine.ChangeState(controller.EnemyTurnState);
            }
            else
            {
                controller.StateMachine.ChangeState(controller.PlayerTurnState);
            }
        }

        public void Exit()
        {
        }
    }
}
