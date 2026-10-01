using UnityEngine;
using JPTJL.Timing;

namespace JPTJL.Data
{
    /// <summary>
    /// ScriptableObject defining combat skills, timing requirements (QTE), and damage metrics.
    /// </summary>
    [CreateAssetMenu(fileName = "NewSkill", menuName = "JPTJL/Combat/Skill Data")]
    public class SkillData : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string skillId = "skill_strike";
        [SerializeField] private string skillName = "Strike";
        [TextArea]
        [SerializeField] private string description = "A standard physical strike requiring timing precision.";

        [Header("Damage & Cost")]
        [SerializeField] private int basePower = 25;
        [SerializeField] private float attackScaling = 1.0f;
        [SerializeField] private int energyCost = 0;

        [Header("Timing / QTE Configuration")]
        [SerializeField] private bool requiresQTE = true;
        [SerializeField] private TimingWindowConfig timingConfig = new TimingWindowConfig(1.0f, 0.65f, 0.06f, 0.16f, false);

        [Header("Timing Multipliers")]
        [Tooltip("Damage multiplier applied on TimingResult.Miss")]
        [SerializeField] private float missMultiplier = 0.5f;

        [Tooltip("Damage multiplier applied on TimingResult.Good")]
        [SerializeField] private float goodMultiplier = 1.15f;

        [Tooltip("Damage multiplier applied on TimingResult.Perfect")]
        [SerializeField] private float perfectMultiplier = 1.5f;

        public string SkillId => skillId;
        public string SkillName => skillName;
        public string Description => description;
        public int BasePower => basePower;
        public float AttackScaling => attackScaling;
        public int EnergyCost => energyCost;
        public bool RequiresQTE => requiresQTE;
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
