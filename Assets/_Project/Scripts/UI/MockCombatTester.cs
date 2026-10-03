using Project.Combat.Data;
using UnityEngine;

namespace Project.Combat.Testing
{
    /// <summary>
    /// Sandbox tester for the combat UI layer.
    /// Simulates input events without requiring a real CombatManager.
    ///
    /// Hotkeys:
    ///   Space → Launch QTE shrink ring (first skill in heroData)
    ///   H     → Deal 15 damage to the hero
    ///   J     → Deal 25 damage to the enemy
    ///
    /// Wire all references in the Inspector. No FindObjectOfType is used.
    /// </summary>
    public class MockCombatTester : MonoBehaviour
    {
        // ─────────────────────────────────────────
        //  Serialized References
        // ─────────────────────────────────────────

        [Header("UI Controllers")]
        [Tooltip("The QTE shrink-ring controller.")]
        [SerializeField] private Project.Combat.UI.TimingIndicatorUI timingUI;

        [Tooltip("Status bar for the player hero.")]
        [SerializeField] private Project.Combat.UI.CombatUnitUI heroUI;

        [Tooltip("Status bar for the enemy.")]
        [SerializeField] private Project.Combat.UI.CombatUnitUI enemyUI;

        [Header("ScriptableObject Data")]
        [Tooltip("Stats SO for the hero character.")]
        [SerializeField] private CharacterStatsData heroData;

        [Tooltip("Stats SO for the enemy character.")]
        [SerializeField] private CharacterStatsData enemyData;

        // ─────────────────────────────────────────
        //  Runtime State
        // ─────────────────────────────────────────

        private int _heroCurrentHp;
        private int _enemyCurrentHp;

        // ─────────────────────────────────────────
        //  Unity Lifecycle
        // ─────────────────────────────────────────

        private void Start()
        {
            if (!ValidateReferences()) return;

            // Snapshot starting HP from SOs into mutable runtime vars.
            _heroCurrentHp  = heroData.maxHP;
            _enemyCurrentHp = enemyData.maxHP;

            // Push initial state to both unit panels.
            heroUI.Initialize(heroData.characterName,   _heroCurrentHp,  heroData.maxHP);
            enemyUI.Initialize(enemyData.characterName, _enemyCurrentHp, enemyData.maxHP);

            Debug.Log("[MockCombatTester] Initialized — Space: QTE | H: Hit Hero | J: Hit Enemy");
        }

        private void Update()
        {
            if (!ValidateReferences()) return;

            HandleQTEInput();
            HandleDamageInput();
        }

        // ─────────────────────────────────────────
        //  Input Handlers
        // ─────────────────────────────────────────

        /// <summary>
        /// Space — Launch the shrink-ring QTE using the hero's first skill.
        /// Falls back gracefully if heroData has no skills assigned.
        /// </summary>
/// <summary>
        /// Space - two-phase toggle:
        ///   - Indicator OFF: start the QTE with the hero's first skill.
        ///   - Indicator ON:  call TryHit() and log the result.
        /// Falls back gracefully if heroData has no skills assigned.
        /// </summary>
        private void HandleQTEInput()
        {
            if (!Input.GetKeyDown(KeyCode.Space)) return;

            // Phase 2: indicator is running - evaluate the player's hit.
            if (timingUI.IsActive)
            {
                Project.Combat.UI.TimingResult resultado = timingUI.TryHit();
                Debug.Log($"Resultado del QTE: {resultado}");
                return;
            }

            // Phase 1: indicator is idle - launch the QTE.
            SkillData skill = heroData.GetSkill(0);

            if (skill == null)
            {
                Debug.LogWarning("[MockCombatTester] heroData has no skills assigned. " +
                                 "Add at least one SkillData to the skills list in the Inspector.");
                return;
            }

            timingUI.ShowIndicator(
                skill.duration,
                skill.goodWindowStart,
                skill.goodWindowEnd,
                skill.perfectWindowStart,
                skill.perfectWindowEnd);

            Debug.Log($"[MockCombatTester] QTE started - Skill: '{skill.skillName}' " +
                      $"| Duration: {skill.duration}s " +
                      $"| Good: [{skill.goodWindowStart:F2}-{skill.goodWindowEnd:F2}] " +
                      $"| Perfect: [{skill.perfectWindowStart:F2}-{skill.perfectWindowEnd:F2}]");
        }

        /// <summary>
        /// H → deal 15 damage to the hero.
        /// J → deal 25 damage to the enemy.
        /// HP is floored at 0 to avoid negative display values.
        /// </summary>
        private void HandleDamageInput()
        {
            if (Input.GetKeyDown(KeyCode.H))
            {
                _heroCurrentHp = Mathf.Max(0, _heroCurrentHp - 15);
                heroUI.UpdateHP(_heroCurrentHp, heroData.maxHP, animate: true);
                Debug.Log($"[MockCombatTester] Hero hit! HP: {_heroCurrentHp} / {heroData.maxHP}");
            }

            if (Input.GetKeyDown(KeyCode.J))
            {
                _enemyCurrentHp = Mathf.Max(0, _enemyCurrentHp - 25);
                enemyUI.UpdateHP(_enemyCurrentHp, enemyData.maxHP, animate: true);
                Debug.Log($"[MockCombatTester] Enemy hit! HP: {_enemyCurrentHp} / {enemyData.maxHP}");
            }
        }

        // ─────────────────────────────────────────
        //  Validation
        // ─────────────────────────────────────────

        /// <summary>
        /// Guards all entry points against missing Inspector references.
        /// Logs a clear error once so the developer can fix it immediately.
        /// </summary>
        private bool ValidateReferences()
        {
            if (timingUI  == null) { LogMissing(nameof(timingUI));  return false; }
            if (heroUI    == null) { LogMissing(nameof(heroUI));    return false; }
            if (enemyUI   == null) { LogMissing(nameof(enemyUI));   return false; }
            if (heroData  == null) { LogMissing(nameof(heroData));  return false; }
            if (enemyData == null) { LogMissing(nameof(enemyData)); return false; }
            return true;
        }

        private static void LogMissing(string fieldName)
            => Debug.LogError($"[MockCombatTester] Missing reference: '{fieldName}'. " +
                              "Assign it in the Inspector.");
    }
}
