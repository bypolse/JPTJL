using Project.Combat.Data;
using Project.Combat.Events;
using Project.Combat.UI;
using UnityEngine;

namespace Project.Combat.Testing
{
    /// <summary>
    /// Sandbox tester for the combat UI layer decoupled via Event Bus (CombatEvents).
    /// Simulates combat events without requiring a full battle manager.
    ///
    /// Controls:
    ///   Space → Trigger QTE / TryHit (two-phase toggle)
    ///   H     → Deal 15 damage to the hero via CombatEvents.UpdateHealth
    ///   J     → Deal 25 damage to the enemy via CombatEvents.UpdateHealth
    /// </summary>
    public class MockCombatTester : MonoBehaviour
    {
        // ─────────────────────────────────────────
        //  Serialized References
        // ─────────────────────────────────────────

        [Header("UI Controllers")]
        [Tooltip("The QTE shrink-ring controller.")]
        [SerializeField] private TimingIndicatorUI timingUI;

        [Tooltip("Status bar for the player hero.")]
        [SerializeField] private CombatUnitUI heroUI;

        [Tooltip("Status bar for the enemy.")]
        [SerializeField] private CombatUnitUI enemyUI;

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

        private void OnEnable()
        {
            CombatEvents.OnQTECompleted += HandleQTECompleted;
        }

        private void OnDisable()
        {
            CombatEvents.OnQTECompleted -= HandleQTECompleted;
        }

        private void Start()
        {
            if (!ValidateReferences()) return;

            // Snapshot starting HP from SOs into mutable runtime vars.
            _heroCurrentHp  = heroData.maxHP;
            _enemyCurrentHp = enemyData.maxHP;

            // Push initial state to both unit panels.
            heroUI.Initialize(heroData.characterName,   _heroCurrentHp,  heroData.maxHP, "Hero");
            enemyUI.Initialize(enemyData.characterName, _enemyCurrentHp, enemyData.maxHP, "Enemy");

            Debug.Log("[MockCombatTester] Initialized via Event Bus — Space: QTE | H: Hit Hero | J: Hit Enemy");
        }

        private void Update()
        {
            if (!ValidateReferences()) return;

            HandleQTEInput();
            HandleDamageInput();
        }

        // ─────────────────────────────────────────
        //  Event Bus Handlers
        // ─────────────────────────────────────────

        private void HandleQTECompleted(TimingResult result)
        {
            if (result == TimingResult.None) return;

            SkillData skill = heroData != null ? heroData.GetSkill(0) : null;
            int baseDamage = skill != null ? skill.baseDamage : (heroData != null ? heroData.baseAttack : 20);

            int damage = 0;
            switch (result)
            {
                case TimingResult.Perfect:
                    damage = Mathf.RoundToInt(baseDamage * 1.5f);
                    break;
                case TimingResult.Good:
                    damage = baseDamage;
                    break;
                case TimingResult.Miss:
                    damage = 0;
                    break;
            }

            if (damage > 0)
            {
                _enemyCurrentHp = Mathf.Max(0, _enemyCurrentHp - damage);
            }

            int maxEnemyHp = enemyData != null ? enemyData.maxHP : 100;
            CombatEvents.UpdateHealth("Enemy", _enemyCurrentHp, maxEnemyHp);

            Debug.Log($"[MockCombatTester] QTE Completed: {result}! Simulated Damage dealt to Enemy: {damage}. Enemy HP: {_enemyCurrentHp} / {maxEnemyHp}");
        }

        // ─────────────────────────────────────────
        //  Input Handlers
        // ─────────────────────────────────────────

        /// <summary>
        /// Space - two-phase toggle:
        ///   - Indicator OFF: triggers CombatEvents.TriggerQTE with hero's first skill.
        ///   - Indicator ON:  calls timingUI.TryHit(), which resolves timing and fires CombatEvents.CompleteQTE.
        /// </summary>
        private void HandleQTEInput()
        {
            if (!Input.GetKeyDown(KeyCode.Space)) return;

            // Phase 2: indicator is active — evaluate player hit.
            if (timingUI.IsActive)
            {
                timingUI.TryHit();
                return;
            }

            // Phase 1: indicator is idle — launch QTE via Event Bus.
            SkillData skill = heroData.GetSkill(0);

            if (skill == null)
            {
                Debug.LogWarning("[MockCombatTester] heroData has no skills assigned. " +
                                 "Add at least one SkillData to the skills list in the Inspector.");
                return;
            }

            if (timingUI != null && !timingUI.gameObject.activeInHierarchy)
            {
                timingUI.gameObject.SetActive(true);
            }

            Debug.Log($"[MockCombatTester] Triggering QTE via Event Bus for skill: '{skill.skillName}'");
            CombatEvents.TriggerQTE(skill);
        }

        /// <summary>
        /// H → deals 15 damage to the hero via CombatEvents.UpdateHealth.
        /// J → deals 25 damage to the enemy via CombatEvents.UpdateHealth.
        /// </summary>
        private void HandleDamageInput()
        {
            if (Input.GetKeyDown(KeyCode.H))
            {
                _heroCurrentHp = Mathf.Max(0, _heroCurrentHp - 15);
                CombatEvents.UpdateHealth("Hero", _heroCurrentHp, heroData.maxHP);
                Debug.Log($"[MockCombatTester] Hero hit via Event Bus! HP: {_heroCurrentHp} / {heroData.maxHP}");
            }

            if (Input.GetKeyDown(KeyCode.J))
            {
                _enemyCurrentHp = Mathf.Max(0, _enemyCurrentHp - 25);
                CombatEvents.UpdateHealth("Enemy", _enemyCurrentHp, enemyData.maxHP);
                Debug.Log($"[MockCombatTester] Enemy hit via Event Bus! HP: {_enemyCurrentHp} / {enemyData.maxHP}");
            }
        }

        // ─────────────────────────────────────────
        //  Validation
        // ─────────────────────────────────────────

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
