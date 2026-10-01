using System.Collections.Generic;
using UnityEngine;

namespace Project.Combat.Data
{
    /// <summary>
    /// Defines the base stats and defensive timing windows for a combatant.
    /// Both player-controlled characters and enemies share this structure,
    /// allowing the combat logic branch to use a single interface.
    /// </summary>
    [CreateAssetMenu(fileName = "New CharacterStatsData", menuName = "Game/Combat/Character Stats Data")]
    public class CharacterStatsData : ScriptableObject
    {
        // ─────────────────────────────────────────
        //  Identity
        // ─────────────────────────────────────────

        [Header("Identity")]
        [Tooltip("Display name used in battle UI and dialogue.")]
        public string characterName;

        [Tooltip("True for party members; false for enemies and NPCs.")]
        public bool isPlayerControlled;

        // ─────────────────────────────────────────
        //  Base Stats
        // ─────────────────────────────────────────

        [Header("Base Stats")]
        [Tooltip("Maximum hit points at level 1 before scaling.")]
        public int maxHP = 100;

        [Tooltip("Base physical or magical damage output before skill multipliers.")]
        public int baseAttack = 15;

        [Tooltip("Damage reduction applied to incoming attacks.")]
        public int baseDefense = 5;

        [Tooltip("Determines turn order in battle. Higher value acts sooner.")]
        public int baseSpeed = 10;

        // ─────────────────────────────────────────
        //  Defensive Timing Windows (in seconds)
        // ─────────────────────────────────────────

        [Header("Defensive Timing Windows")]
        [Tooltip(
            "Seconds after a Parry prompt appears during which a successful Parry input is accepted. " +
            "Shorter values make Parry harder.")]
        public float parryWindowDuration = 0.15f;

        [Tooltip(
            "Seconds after a Dodge prompt appears during which a successful Dodge input is accepted. " +
            "Typically larger than parryWindowDuration to allow a softer defensive option.")]
        public float dodgeWindowDuration = 0.30f;

        // ─────────────────────────────────────────
        //  Skill Loadout
        // ─────────────────────────────────────────

        [Header("Skill Loadout")]
        [Tooltip("Ordered list of skills available to this character in battle.")]
        public List<SkillData> skills = new List<SkillData>();

        // ─────────────────────────────────────────
        //  Public Helpers
        // ─────────────────────────────────────────

        /// <summary>
        /// Returns true if the character has at least one skill assigned.
        /// </summary>
        public bool HasSkills => skills != null && skills.Count > 0;

        /// <summary>
        /// Safely retrieves a skill by index.
        /// Returns null if the index is out of range.
        /// </summary>
        /// <param name="index">Zero-based skill index.</param>
        public SkillData GetSkill(int index)
        {
            if (skills == null || index < 0 || index >= skills.Count)
                return null;

            return skills[index];
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            // Clamp stats to sensible minimums to prevent division-by-zero
            // or negative values in the combat logic branch.
            maxHP         = Mathf.Max(1, maxHP);
            baseAttack    = Mathf.Max(0, baseAttack);
            baseDefense   = Mathf.Max(0, baseDefense);
            baseSpeed     = Mathf.Max(1, baseSpeed);

            // Timing windows must be positive.
            parryWindowDuration = Mathf.Max(0.01f, parryWindowDuration);
            dodgeWindowDuration = Mathf.Max(0.01f, dodgeWindowDuration);
        }
#endif
    }
}
