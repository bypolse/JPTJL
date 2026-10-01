using UnityEngine;

namespace Project.Combat.Data
{
    /// <summary>
    /// Defines all static data for a combat skill, including base stats,
    /// targeting type, and normalized QTE timing windows for active input mechanics.
    /// </summary>
    [CreateAssetMenu(fileName = "New SkillData", menuName = "Game/Combat/Skill Data")]
    public class SkillData : ScriptableObject
    {
        // ─────────────────────────────────────────
        //  Identity
        // ─────────────────────────────────────────

        [Header("Identity")]
        [Tooltip("Unique runtime identifier. Must be consistent with save/load systems.")]
        public string skillId;

        [Tooltip("Display name shown in menus and battle UI.")]
        public string skillName;

        [Tooltip("Short description shown in skill detail panels.")]
        [TextArea(2, 4)]
        public string description;

        [Tooltip("Icon displayed in battle menus and skill lists.")]
        public Sprite icon;

        // ─────────────────────────────────────────
        //  Combat Values
        // ─────────────────────────────────────────

        [Header("Combat Values")]
        [Tooltip("MP or equivalent resource consumed on use.")]
        public int resourceCost;

        [Tooltip("Base damage dealt before Defense and multipliers are applied.")]
        public int baseDamage;

        [Tooltip("Base HP restored before multipliers are applied. Set 0 if not a heal.")]
        public int baseHeal;

        [Tooltip("Determines who can be targeted when this skill is selected.")]
        public TargetType targetType;

        // ─────────────────────────────────────────
        //  QTE Timing Configuration
        // ─────────────────────────────────────────

        [Header("QTE Timing Windows")]
        [Tooltip("Total duration of the QTE animation in seconds.")]
        public float duration = 1.2f;

        [Space]
        [Tooltip("Normalized start of the 'Good' timing window (0 = start, 1 = end of QTE).")]
        [Range(0f, 1f)] public float goodWindowStart = 0.60f;

        [Tooltip("Normalized end of the 'Good' timing window.")]
        [Range(0f, 1f)] public float goodWindowEnd = 0.95f;

        [Space]
        [Tooltip("Normalized start of the 'Perfect' timing window. Must be inside the Good window.")]
        [Range(0f, 1f)] public float perfectWindowStart = 0.75f;

        [Tooltip("Normalized end of the 'Perfect' timing window. Must be inside the Good window.")]
        [Range(0f, 1f)] public float perfectWindowEnd = 0.85f;

        // ─────────────────────────────────────────
        //  Public Helpers
        // ─────────────────────────────────────────

        /// <summary>
        /// Evaluates a normalized input time [0,1] and returns the corresponding QTE result.
        /// Called by UI/feedback systems — this SO never modifies game state.
        /// </summary>
        /// <param name="normalizedTime">Input time normalized between 0 and 1.</param>
        /// <returns>The QTE hit quality for this input time.</returns>
        public QTEResult EvaluateTiming(float normalizedTime)
        {
            if (normalizedTime >= perfectWindowStart && normalizedTime <= perfectWindowEnd)
                return QTEResult.Perfect;

            if (normalizedTime >= goodWindowStart && normalizedTime <= goodWindowEnd)
                return QTEResult.Good;

            return QTEResult.Miss;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            // Enforce that Perfect window stays nested inside the Good window.
            perfectWindowStart = Mathf.Clamp(perfectWindowStart, goodWindowStart, goodWindowEnd);
            perfectWindowEnd   = Mathf.Clamp(perfectWindowEnd,   goodWindowStart, goodWindowEnd);

            // Ensure end > start for both windows.
            if (goodWindowEnd <= goodWindowStart)
                goodWindowEnd = goodWindowStart + 0.01f;

            if (perfectWindowEnd <= perfectWindowStart)
                perfectWindowEnd = perfectWindowStart + 0.01f;
        }
#endif
    }

    // ─────────────────────────────────────────
    //  Shared Enums
    // ─────────────────────────────────────────

    /// <summary>
    /// Defines who a skill can target in battle.
    /// </summary>
    public enum TargetType
    {
        SingleEnemy,
        AllEnemies,
        SingleAlly,
        Self
    }

    /// <summary>
    /// Result categories for QTE input evaluation.
    /// Consumed by UI feedback and the combat logic branch via events.
    /// </summary>
    public enum QTEResult
    {
        Miss,
        Good,
        Perfect
    }
}
