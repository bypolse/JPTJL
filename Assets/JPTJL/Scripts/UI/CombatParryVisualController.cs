using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using JPTJL.Combat;
using JPTJL.Data;
using JPTJL.Timing;

namespace JPTJL.UI
{
    /// <summary>
    /// Controls the enemy attack animation, the high-precision red dot parry indicator ("Punto Rojo"),
    /// and visual feedback when a Parry deflects enemy damage.
    /// </summary>
    public class CombatParryVisualController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private CombatController combatController;
        [SerializeField] private RectTransform enemyPlatform;
        [SerializeField] private RectTransform heroPlatform;
        [SerializeField] private RectTransform enemyUnitUIRoot;

        [Header("Red Dot Parry Indicator")]
        [Tooltip("The container or image for the exact millisecond parry indicator.")]
        [SerializeField] private GameObject parryRedDotRoot;
        [SerializeField] private Image redDotImage;
        [SerializeField] private TextMeshProUGUI parryPromptText;
        [SerializeField] private Button parryClickButton;

        [Header("Parry Flash / Shield Effect")]
        [SerializeField] private GameObject parryShieldFlash;

        [Header("Configuration")]
        [SerializeField] private Vector2 parryDotPosition = new Vector2(-160f, -80f);
        [SerializeField] private float parryDotScale = 1.0f;

        private Vector2 originalEnemyPlatformPos = new Vector2(480f, -90f);
        private Coroutine attackAnimationRoutine;
        private Coroutine redDotRoutine;
        private Coroutine shieldFlashRoutine;
        private bool isDefenseWindowActive = false;

        private void Awake()
        {
            if (combatController == null)
            {
                combatController = FindFirstObjectByType<CombatController>();
            }

            ResolvePlatformReferences();

            if (parryRedDotRoot != null) parryRedDotRoot.SetActive(false);
            if (parryShieldFlash != null) parryShieldFlash.SetActive(false);

            if (parryClickButton != null)
            {
                parryClickButton.onClick.RemoveAllListeners();
                parryClickButton.onClick.AddListener(OnParryButtonClicked);
            }
        }

        private void OnEnable()
        {
            CombatEvents.OnDefenseWindowStarted += HandleDefenseWindowStarted;
            CombatEvents.OnDefenseWindowResolved += HandleDefenseWindowResolved;
            CombatEvents.OnDefenseWindowTicked += HandleDefenseWindowTicked;
            CombatEvents.OnPhaseChanged += HandlePhaseChanged;
        }

        private void OnDisable()
        {
            CombatEvents.OnDefenseWindowStarted -= HandleDefenseWindowStarted;
            CombatEvents.OnDefenseWindowResolved -= HandleDefenseWindowResolved;
            CombatEvents.OnDefenseWindowTicked -= HandleDefenseWindowTicked;
            CombatEvents.OnPhaseChanged -= HandlePhaseChanged;

            StopAllCoroutines();
            ResetEnemyPosition();
            if (parryRedDotRoot != null) parryRedDotRoot.SetActive(false);
        }

        public void ResolvePlatformReferences()
        {
            if (enemyPlatform == null)
            {
                var epGo = GameObject.Find("EnemyPlatform");
                if (epGo != null) enemyPlatform = epGo.GetComponent<RectTransform>();
            }
            if (heroPlatform == null)
            {
                var hpGo = GameObject.Find("HeroPlatform");
                if (hpGo != null) heroPlatform = hpGo.GetComponent<RectTransform>();
            }
            if (enemyPlatform != null)
            {
                originalEnemyPlatformPos = enemyPlatform.anchoredPosition;
            }
        }

        public void SetupComponents(
            CombatController ctrl,
            RectTransform ePlatform,
            RectTransform hPlatform,
            GameObject redDotRoot,
            Image dotImg,
            TextMeshProUGUI promptText,
            Button clickBtn,
            GameObject shieldFlashGo)
        {
            combatController = ctrl;
            enemyPlatform = ePlatform;
            heroPlatform = hPlatform;
            parryRedDotRoot = redDotRoot;
            redDotImage = dotImg;
            parryPromptText = promptText;
            parryClickButton = clickBtn;
            parryShieldFlash = shieldFlashGo;

            if (enemyPlatform != null)
            {
                originalEnemyPlatformPos = enemyPlatform.anchoredPosition;
            }

            if (parryRedDotRoot != null) parryRedDotRoot.SetActive(false);
            if (parryShieldFlash != null) parryShieldFlash.SetActive(false);

            if (parryClickButton != null)
            {
                parryClickButton.onClick.RemoveAllListeners();
                parryClickButton.onClick.AddListener(OnParryButtonClicked);
            }
        }

        private void OnParryButtonClicked()
        {
            if (combatController != null && isDefenseWindowActive)
            {
                combatController.TriggerDefenseInput(DefenseType.Parry);
            }
        }

        private void HandlePhaseChanged(CombatPhase phase)
        {
            if (phase != CombatPhase.DefenseWindow && phase != CombatPhase.EnemyTurn)
            {
                isDefenseWindowActive = false;
                if (parryRedDotRoot != null) parryRedDotRoot.SetActive(false);
                ResetEnemyPosition();
            }
        }

        private void HandleDefenseWindowStarted(DefenseType type, TimingWindowConfig config)
        {
            isDefenseWindowActive = true;
            ResolvePlatformReferences();

            if (attackAnimationRoutine != null) StopCoroutine(attackAnimationRoutine);
            attackAnimationRoutine = StartCoroutine(PlayEnemyAttackAnimationRoutine(config));

            if (redDotRoutine != null) StopCoroutine(redDotRoutine);
            redDotRoutine = StartCoroutine(PlayRedDotTimingRoutine(config));
        }

        private void HandleDefenseWindowTicked(DefenseType type, float progress, float elapsedTime)
        {
            // Optional continuous adjustments
        }

        private void HandleDefenseWindowResolved(DefenseType type, TimingResult result)
        {
            isDefenseWindowActive = false;

            if (result == TimingResult.Perfect && type == DefenseType.Parry)
            {
                // Successful Perfect Parry!
                TriggerParrySuccessFeedback();
            }

            if (parryRedDotRoot != null)
            {
                parryRedDotRoot.SetActive(false);
            }
        }

        /// <summary>
        /// Animates the enemy lunging toward the hero and returning after the strike.
        /// </summary>
        private IEnumerator PlayEnemyAttackAnimationRoutine(TimingWindowConfig config)
        {
            if (enemyPlatform == null) yield break;

            Vector2 startPos = originalEnemyPlatformPos;
            Vector2 targetLungePos = heroPlatform != null 
                ? (heroPlatform.anchoredPosition + new Vector2(250f, 0f))
                : new Vector2(-150f, -90f);

            float targetTime = config.TargetTime; // Impact instant (e.g. 0.50s)
            float totalDuration = config.TotalDuration; // Total animation (e.g. 0.85s)

            float elapsed = 0f;

            // Phase 1: Wind-up / Anticipation (0.0s -> ~0.25s): Enemy pulls back slightly and shakes
            float windupDuration = targetTime * 0.45f;
            while (elapsed < windupDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / windupDuration);
                float shake = Mathf.Sin(elapsed * 45f) * 6f;
                enemyPlatform.anchoredPosition = startPos + new Vector2(20f * t, shake);
                yield return null;
            }

            // Phase 2: Lunge forward to strike hero (windup -> targetTime)
            float lungeDuration = targetTime - windupDuration;
            float lungeTimer = 0f;
            Vector2 preLungePos = enemyPlatform.anchoredPosition;

            while (lungeTimer < lungeDuration)
            {
                lungeTimer += Time.deltaTime;
                float t = Mathf.Clamp01(lungeTimer / lungeDuration);
                // Ease In Quad (accelerates fast into the strike)
                float curvedT = t * t;
                enemyPlatform.anchoredPosition = Vector2.Lerp(preLungePos, targetLungePos, curvedT);
                yield return null;
            }

            enemyPlatform.anchoredPosition = targetLungePos;

            // Phase 3: Recovery / Return back to original position (targetTime -> totalDuration)
            float recoveryDuration = Mathf.Max(0.1f, totalDuration - targetTime);
            float recoveryTimer = 0f;

            while (recoveryTimer < recoveryDuration)
            {
                recoveryTimer += Time.deltaTime;
                float t = Mathf.Clamp01(recoveryTimer / recoveryDuration);
                // Ease Out Back/Cubic
                float curvedT = 1f - Mathf.Pow(1f - t, 3);
                enemyPlatform.anchoredPosition = Vector2.Lerp(targetLungePos, startPos, curvedT);
                yield return null;
            }

            enemyPlatform.anchoredPosition = startPos;
        }

        /// <summary>
        /// Displays the glowing red dot at the exact millisecond window of the parry.
        /// </summary>
        private IEnumerator PlayRedDotTimingRoutine(TimingWindowConfig config)
        {
            if (parryRedDotRoot == null) yield break;

            float targetTime = config.TargetTime;
            float threshold = config.PerfectThreshold;

            float parryWindowStart = Mathf.Max(0f, targetTime - threshold);
            float parryWindowEnd = targetTime + threshold;

            float timer = 0f;
            parryRedDotRoot.SetActive(false);

            // Wait until the exact parry window starts
            while (timer < parryWindowStart)
            {
                timer += Time.deltaTime;
                yield return null;
            }

            // ─── EXACT PARRY WINDOW ACTIVE! ("PUNTO ROJO") ───
            parryRedDotRoot.SetActive(true);
            parryRedDotRoot.transform.localScale = Vector3.one * (parryDotScale * 1.4f);

            if (redDotImage != null)
            {
                redDotImage.color = new Color(1f, 0.1f, 0.15f, 1f);
            }

            if (parryPromptText != null)
            {
                parryPromptText.text = "<b><color=#FF2233>● ¡PARRY!</color></b>\n<size=14><color=#FFFF88>[Z / Clic]</color></size>";
            }

            // Pulse intensely during the exact parry window
            while (timer < parryWindowEnd && isDefenseWindowActive)
            {
                timer += Time.deltaTime;
                float windowT = (timer - parryWindowStart) / Mathf.Max(0.01f, parryWindowEnd - parryWindowStart);
                float pulse = 1f + 0.35f * Mathf.Sin(windowT * Mathf.PI);

                parryRedDotRoot.transform.localScale = Vector3.one * (parryDotScale * pulse);
                yield return null;
            }

            // Window closed
            if (parryRedDotRoot != null)
            {
                parryRedDotRoot.SetActive(false);
            }
        }

        private void TriggerParrySuccessFeedback()
        {
            if (shieldFlashRoutine != null) StopCoroutine(shieldFlashRoutine);
            shieldFlashRoutine = StartCoroutine(PlayParryShieldFlashRoutine());

            CombatEvents.TriggerFloatingFeedback("🛡️ ¡PARRY PERFECTO!\n¡Todo el daño desviado!", new Color(1f, 0.85f, 0.2f));
        }

        private IEnumerator PlayParryShieldFlashRoutine()
        {
            if (parryShieldFlash == null) yield break;

            parryShieldFlash.SetActive(true);
            CanvasGroup cg = parryShieldFlash.GetComponent<CanvasGroup>();
            RectTransform rt = parryShieldFlash.GetComponent<RectTransform>();

            float duration = 0.4f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;

                if (rt != null)
                {
                    rt.localScale = Vector3.Lerp(new Vector3(0.5f, 0.5f, 1f), new Vector3(2.2f, 2.2f, 1f), t);
                }

                if (cg != null)
                {
                    cg.alpha = Mathf.Lerp(1f, 0f, t);
                }

                yield return null;
            }

            parryShieldFlash.SetActive(false);
        }

        private void ResetEnemyPosition()
        {
            if (enemyPlatform != null)
            {
                enemyPlatform.anchoredPosition = originalEnemyPlatformPos;
            }
        }
    }
}
