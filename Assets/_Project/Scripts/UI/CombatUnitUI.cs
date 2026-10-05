using System.Collections;
using Project.Combat.Events;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Project.Combat.UI
{
    /// <summary>
    /// Displays and animates the status bar for a single combat unit (hero or enemy).
    /// Listens to CombatEvents.OnUnitHealthChanged to update reactivity via Event Bus,
    /// or receives direct atomic updates via public methods.
    /// </summary>
    public class CombatUnitUI : MonoBehaviour
    {
        // ─────────────────────────────────────────
        //  Serialized References
        // ─────────────────────────────────────────

        [Header("Unit Info")]
        [Tooltip("Unique identifier matching CombatEvents targetUnitId.")]
        [SerializeField] private string unitId;

        [Tooltip("Label that displays the unit's name.")]
        [SerializeField] private TextMeshProUGUI nameText;

        [Header("HP Bar")]
        [Tooltip("Slider displaying current HP.")]
        [SerializeField] private Slider hpSlider;

        [FormerlySerializedAs("hpNumericText")]
        [Tooltip("Label showing current and max HP in 'current / max' format.")]
        [SerializeField] private TextMeshProUGUI hpText;

        // ─────────────────────────────────────────
        //  Private State
        // ─────────────────────────────────────────

        private const float HP_ANIM_DURATION = 0.25f;
        private Coroutine _hpAnimRoutine;

        public string UnitId
        {
            get => unitId;
            set => unitId = value;
        }

        // ─────────────────────────────────────────
        //  Unity Lifecycle
        // ─────────────────────────────────────────

        private void OnEnable()
        {
            CombatEvents.OnUnitHealthChanged += HandleUnitHealthChanged;
        }

        private void OnDisable()
        {
            CombatEvents.OnUnitHealthChanged -= HandleUnitHealthChanged;
        }

        // ─────────────────────────────────────────
        //  Event Bus Handler
        // ─────────────────────────────────────────

        private void HandleUnitHealthChanged(string targetUnitId, int currentHp, int maxHp)
        {
            if (string.Equals(targetUnitId, unitId, System.StringComparison.OrdinalIgnoreCase)
                || (nameText != null && string.Equals(targetUnitId, nameText.text, System.StringComparison.OrdinalIgnoreCase)))
            {
                UpdateHP(currentHp, maxHp, animate: true);
            }
        }

        // ─────────────────────────────────────────
        //  Public API
        // ─────────────────────────────────────────

        /// <summary>
        /// Sets up the unit panel with its initial values.
        /// </summary>
        public void Initialize(string unitName, int currentHp, int maxHp, string id = null)
        {
            if (nameText != null)
                nameText.text = unitName;

            if (!string.IsNullOrEmpty(id))
                unitId = id;
            else if (string.IsNullOrEmpty(unitId))
                unitId = unitName;

            if (hpSlider != null)
            {
                hpSlider.minValue = 0f;
                hpSlider.maxValue = maxHp;
                hpSlider.value    = Mathf.Clamp(currentHp, 0, maxHp);
            }

            if (hpText != null)
                hpText.text = FormatHP(currentHp, maxHp);
        }

        /// <summary>
        /// Updates the HP display. If animate is true, smoothly interpolates the slider.
        /// </summary>
        public void UpdateHP(int currentHp, int maxHp, bool animate = true)
        {
            int clampedHp = Mathf.Clamp(currentHp, 0, maxHp);

            if (hpSlider != null)
            {
                hpSlider.minValue = 0f;
                hpSlider.maxValue = maxHp;
            }

            if (hpText != null)
                hpText.text = FormatHP(clampedHp, maxHp);

            if (hpSlider == null) return;

            if (animate)
            {
                if (_hpAnimRoutine != null)
                    StopCoroutine(_hpAnimRoutine);

                _hpAnimRoutine = StartCoroutine(AnimateSlider(hpSlider.value, clampedHp));
            }
            else
            {
                if (_hpAnimRoutine != null)
                {
                    StopCoroutine(_hpAnimRoutine);
                    _hpAnimRoutine = null;
                }

                hpSlider.value = clampedHp;
            }
        }

        // ─────────────────────────────────────────
        //  Coroutines
        // ─────────────────────────────────────────

        private IEnumerator AnimateSlider(float fromValue, float toValue)
        {
            float elapsed = 0f;

            while (elapsed < HP_ANIM_DURATION)
            {
                elapsed += Time.deltaTime;

                float t = Mathf.SmoothStep(0f, 1f, elapsed / HP_ANIM_DURATION);
                hpSlider.value = Mathf.Lerp(fromValue, toValue, t);

                yield return null;
            }

            hpSlider.value = toValue;
            _hpAnimRoutine = null;
        }

        // ─────────────────────────────────────────
        //  Helpers
        // ─────────────────────────────────────────

        private static string FormatHP(int currentHp, int maxHp)
            => $"{Mathf.Max(0, currentHp)} / {Mathf.Max(0, maxHp)}";
    }
}
