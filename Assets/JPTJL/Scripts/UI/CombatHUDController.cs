using System.Collections;
using UnityEngine;
using JPTJL.Combat;
using JPTJL.Data;
using JPTJL.Timing;
using Project.Combat.UI;

namespace JPTJL.UI
{
    /// <summary>
    /// Bridges the core turn-based combat system (CombatController and CombatEvents)
    /// with the modern UGUI Canvas elements (CombatUnitUI health bars and TimingIndicatorUI ring).
    /// Supports automatic reference resolution and reactive updates.
    /// </summary>
    public class CombatHUDController : MonoBehaviour
    {
        [Header("Combat Core References")]
        [SerializeField] private CombatController combatController;

        [Header("UI Element References")]
        [Tooltip("Animated status bar for the player hero.")]
        [SerializeField] private CombatUnitUI heroUI;

        [Tooltip("Animated status bar for the enemy combatant.")]
        [SerializeField] private CombatUnitUI enemyUI;

        [Tooltip("Shrinking ring visual for timing and QTE prompts.")]
        [SerializeField] private TimingIndicatorUI timingUI;

        [Tooltip("Interactive bottom action deck & submenus.")]
        [SerializeField] private CombatActionMenuUI actionMenuUI;

        [Header("Portraits")]
        [SerializeField] private Sprite heroPortrait;
        [SerializeField] private Sprite enemyPortrait;

        [Header("Configuration")]
        [Tooltip("Automatically finds UI references in the scene or under this GameObject if unassigned.")]
        [SerializeField] private bool autoFindReferences = true;

        [Header("Timing Window Defaults")]
        [Range(0f, 1f)] [SerializeField] private float defaultGoodStart = 0.60f;
        [Range(0f, 1f)] [SerializeField] private float defaultGoodEnd = 0.95f;
        [Range(0f, 1f)] [SerializeField] private float defaultPerfectStart = 0.75f;
        [Range(0f, 1f)] [SerializeField] private float defaultPerfectEnd = 0.85f;

        private bool isInitialized = false;

        private void Awake()
        {
            if (autoFindReferences)
            {
                ResolveReferences();
            }
        }

        private void OnEnable()
        {
            CombatEvents.OnTurnStarted += HandleTurnStarted;
            CombatEvents.OnHealthChanged += HandleHealthChanged;
            CombatEvents.OnManaChanged += HandleManaChanged;
            CombatEvents.OnKeyQTEPrompt += HandleKeyQTEPrompt;
            CombatEvents.OnKeyQTEEvaluated += HandleKeyQTEEvaluated;
            CombatEvents.OnFloatingFeedback += HandleFloatingFeedback;
            CombatEvents.OnPhaseChanged += HandlePhaseChanged;
            CombatEvents.OnBattleFinished += HandleBattleFinished;
        }

        private void OnDisable()
        {
            CombatEvents.OnTurnStarted -= HandleTurnStarted;
            CombatEvents.OnHealthChanged -= HandleHealthChanged;
            CombatEvents.OnManaChanged -= HandleManaChanged;
            CombatEvents.OnKeyQTEPrompt -= HandleKeyQTEPrompt;
            CombatEvents.OnKeyQTEEvaluated -= HandleKeyQTEEvaluated;
            CombatEvents.OnFloatingFeedback -= HandleFloatingFeedback;
            CombatEvents.OnPhaseChanged -= HandlePhaseChanged;
            CombatEvents.OnBattleFinished -= HandleBattleFinished;
        }

        private void Start()
        {
            if (!isInitialized)
            {
                TryInitializeUnits();
            }
        }

        /// <summary>
        /// Attempts to find references dynamically if they were not wired in the inspector.
        /// </summary>
        public void ResolveReferences()
        {
            if (combatController == null)
            {
                combatController = FindFirstObjectByType<CombatController>();
            }

            if (timingUI == null)
            {
                timingUI = FindFirstObjectByType<TimingIndicatorUI>();
            }

            if (heroUI == null || enemyUI == null)
            {
                CombatUnitUI[] unitUIs = FindObjectsByType<CombatUnitUI>(FindObjectsSortMode.None);
                foreach (var unitUI in unitUIs)
                {
                    string objName = unitUI.gameObject.name.ToLower();
                    if (heroUI == null && (objName.Contains("hero") || objName.Contains("player")))
                    {
                        heroUI = unitUI;
                    }
                    else if (enemyUI == null && objName.Contains("enemy"))
                    {
                        enemyUI = unitUI;
                    }
                }

                // Fallback by order if names don't match
                if (heroUI == null && unitUIs.Length > 0) heroUI = unitUIs[0];
                if (enemyUI == null && unitUIs.Length > 1) enemyUI = unitUIs[1];
            }

            if (actionMenuUI == null)
            {
                actionMenuUI = FindFirstObjectByType<CombatActionMenuUI>();
            }

            if (actionMenuUI == null || heroUI == null || enemyUI == null || FindFirstObjectByType<CombatParryVisualController>() == null)
            {
                Canvas canvas = FindFirstObjectByType<Canvas>();
                if (canvas != null)
                {
                    actionMenuUI = RuntimeCombatUIBuilder.BuildOrUpdateSketchUI(canvas, ref heroUI, ref enemyUI, ref heroPortrait, ref enemyPortrait);
                }
            }

            if (heroPortrait == null)
            {
                heroPortrait = Resources.Load<Sprite>("UI/icon_hero");
            }

            if (enemyPortrait == null)
            {
                enemyPortrait = Resources.Load<Sprite>("UI/icon_enemy");
            }
        }

        private void TryInitializeUnits()
        {
            if (combatController == null) return;

            if (combatController.Player != null && heroUI != null)
            {
                heroUI.Initialize(
                    combatController.Player.Name,
                    combatController.Player.CurrentHealth,
                    combatController.Player.MaxHealth,
                    combatController.Player.CurrentMana,
                    combatController.Player.MaxMana,
                    heroPortrait
                );
            }

            if (combatController.Enemy != null && enemyUI != null)
            {
                enemyUI.Initialize(
                    combatController.Enemy.Name,
                    combatController.Enemy.CurrentHealth,
                    combatController.Enemy.MaxHealth,
                    0, 0,
                    enemyPortrait
                );
            }

            if (combatController.Player != null && combatController.Enemy != null)
            {
                isInitialized = true;
            }
        }

        private void HandleTurnStarted(CombatParticipant participant)
        {
            if (!isInitialized)
            {
                TryInitializeUnits();
            }
        }

        private void HandleHealthChanged(CombatParticipant participant, int currentHp, int maxHp)
        {
            if (participant == null) return;

            if (participant.IsPlayer)
            {
                if (heroUI != null)
                {
                    heroUI.UpdateHP(currentHp, maxHp, animate: true);
                }
            }
            else
            {
                if (enemyUI != null)
                {
                    enemyUI.UpdateHP(currentHp, maxHp, animate: true);
                }
            }
        }

        private void HandleManaChanged(CombatParticipant participant, int currentMp, int maxMp)
        {
            if (participant == null) return;

            if (participant.IsPlayer && heroUI != null)
            {
                heroUI.UpdateMana(currentMp, maxMp, animate: true);
            }
        }

        private void HandleKeyQTEPrompt(KeyCode key, string keyName, float duration, int currentHit, int totalHits)
        {
            if (timingUI == null) return;

            float gStart = defaultGoodStart;
            float gEnd = defaultGoodEnd;
            float pStart = defaultPerfectStart;
            float pEnd = defaultPerfectEnd;

            // If current skill has precision window data, adapt normalized bounds
            if (combatController != null && combatController.CurrentSkill != null)
            {
                var timingConfig = combatController.CurrentSkill.TimingConfig;
                if (timingConfig.TotalDuration > 0f && timingConfig.TargetTime > 0f)
                {
                    float totalDur = timingConfig.TotalDuration;
                    gStart = Mathf.Clamp01((timingConfig.TargetTime - timingConfig.GoodThreshold) / totalDur);
                    gEnd = Mathf.Clamp01((timingConfig.TargetTime + timingConfig.GoodThreshold) / totalDur);
                    pStart = Mathf.Clamp01((timingConfig.TargetTime - timingConfig.PerfectThreshold) / totalDur);
                    pEnd = Mathf.Clamp01((timingConfig.TargetTime + timingConfig.PerfectThreshold) / totalDur);
                }
            }

            timingUI.ShowIndicator(duration, gStart, gEnd, pStart, pEnd);
        }

        private void HandleKeyQTEEvaluated(KeyCode key, bool success, JPTJL.Combat.TimingResult result, string feedbackText, int currentHit, int totalHits)
        {
            if (timingUI == null) return;

            if (timingUI.IsActive)
            {
                timingUI.TryHit();
            }

            if (!string.IsNullOrEmpty(feedbackText))
            {
                Color feedbackColor = success ? Color.green : Color.red;
                if (result == JPTJL.Combat.TimingResult.Good)
                {
                    feedbackColor = Color.yellow;
                }
                timingUI.DisplayFeedback(feedbackText, feedbackColor);
            }
        }

        private void HandleFloatingFeedback(string text, Color color)
        {
            if (timingUI != null && !string.IsNullOrEmpty(text))
            {
                timingUI.DisplayFeedback(text, color);
            }
        }

        private void HandlePhaseChanged(CombatPhase phase)
        {
            if (phase != CombatPhase.QTEExecution)
            {
                if (timingUI != null && timingUI.IsActive)
                {
                    timingUI.HideIndicator();
                }
            }
        }

        private void HandleBattleFinished(bool playerWon)
        {
            if (timingUI != null)
            {
                string text = playerWon ? "¡VICTORIA!" : "DERROTA";
                Color color = playerWon ? Color.green : Color.red;
                timingUI.DisplayFeedback(text, color);
            }
        }
    }
}
