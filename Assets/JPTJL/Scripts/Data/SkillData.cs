using UnityEngine;
using JPTJL.Timing;

namespace JPTJL.Data
{
    public enum SkillType
    {
        Offensive,
        Shield,
        Heal
    }

    /// <summary>
    /// ScriptableObject defining combat skills, timing requirements (QTE), mana costs, and damage metrics.
    /// </summary>
    [CreateAssetMenu(fileName = "NewSkill", menuName = "JPTJL/Combat/Skill Data")]
    public class SkillData : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string skillId = "skill_strike";
        [SerializeField] private string skillName = "Strike";
        [TextArea]
        [SerializeField] private string description = "A standard physical strike requiring timing precision.";

        [Header("Skill Type & Behavior")]
        [SerializeField] private SkillType skillType = SkillType.Offensive;
        [SerializeField] private int hitCount = 1;
        [SerializeField] private int shieldAmount = 0;

        [Header("Damage & Cost")]
        [SerializeField] private int basePower = 10;
        [SerializeField] private float attackScaling = 0.0f;
        [SerializeField] private int manaCost = 5;
        [SerializeField] private int energyCost = 0; // Legacy fallback

        [Header("Timing / QTE Configuration")]
        [SerializeField] private bool requiresQTE = true;
        [SerializeField] private int qteKeyCount = 1;
        [SerializeField] private float qteDuration = 1.2f;
        [SerializeField] private TimingWindowConfig timingConfig = new TimingWindowConfig(1.2f, 0.60f, 0.06f, 0.16f, false);

        [Header("Timing Multipliers")]
        [Tooltip("Damage multiplier applied on TimingResult.Miss or Normal execution (Base damage = 1.0)")]
        [SerializeField] private float missMultiplier = 1.0f;

        [Tooltip("Damage multiplier applied on TimingResult.Good")]
        [SerializeField] private float goodMultiplier = 1.0f;

        [Tooltip("Damage multiplier applied on TimingResult.Perfect (+20% extra damage = 1.20)")]
        [SerializeField] private float perfectMultiplier = 1.20f;

        public string SkillId => skillId;
        public string SkillName => skillName;
        public string Description => description;
        public SkillType Type => skillType;
        public int HitCount => Mathf.Max(1, hitCount);
        public int ShieldAmount => shieldAmount;
        public int BasePower => basePower;
        public float AttackScaling => attackScaling;
        public int ManaCost => manaCost > 0 ? manaCost : energyCost;
        public int EnergyCost => ManaCost;
        public bool RequiresQTE => requiresQTE;
        public int QteKeyCount => qteKeyCount > 0 ? qteKeyCount : HitCount;
        public float QteDuration => qteDuration > 0 ? qteDuration : 1.2f;
        public TimingWindowConfig TimingConfig => timingConfig;
        public float MissMultiplier => missMultiplier;
        public float GoodMultiplier => goodMultiplier;
        public float PerfectMultiplier => perfectMultiplier;

        public float GetTimingMultiplier(Combat.TimingResult timingResult)
        {
            switch (timingResult)
            {
                case Combat.TimingResult.Perfect:
                    return perfectMultiplier;
                case Combat.TimingResult.Good:
                    return goodMultiplier;
                case Combat.TimingResult.Miss:
                default:
                    return missMultiplier;
            }
        }
    }
}
