using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Project.Combat.UI
{
    /// <summary>
    /// Displays and animates the status bar for a single combat unit
    /// (hero or enemy). Receives atomic updates via public methods and
    /// never polls game state or uses FindObjectOfType.
    /// </summary>
    public class CombatUnitUI : MonoBehaviour
    {
        // ─────────────────────────────────────────
        //  Serialized References
        // ─────────────────────────────────────────

        [Header("Unit Info")]
        [Tooltip("Label that displays the unit's name.")]
        [SerializeField] private TextMeshProUGUI nameText;

        [Header("HP Bar")]
        [Tooltip("Slider whose value represents current HP as a 0–1 ratio.")]
        [SerializeField] private Slider hpSlider;

        [Tooltip("Label showing current and max HP in 'current / max' format.")]
        [SerializeField] private TextMeshProUGUI hpNumericText;

        // ─────────────────────────────────────────
        //  Private State
        // ─────────────────────────────────────────

        /// <summary>Duration in seconds for the animated HP bar transition.</summary>
        private const float HP_ANIM_DURATION = 0.25f;

        private Coroutine _hpAnimRoutine;

        // ─────────────────────────────────────────
        //  Public API
        // ─────────────────────────────────────────

        /// <summary>
        /// Sets up the unit panel with its initial values.
        /// Call once when the battle starts or when this panel is reused.
        /// </summary>
        /// <param name="unitName">Name displayed in the header label.</param>
        /// <param name="currentHp">Starting HP value.</param>
        /// <param name="maxHp">Maximum HP used to normalize the slider.</param>
        public void Initialize(string unitName, int currentHp, int maxHp)
        {
            nameText.text = unitName;

            // Snap to initial values without animation on setup.
            hpSlider.value   = NormalizeHP(currentHp, maxHp);
            hpNumericText.text = FormatHP(currentHp, maxHp);
        }

        /// <summary>
        /// Updates the HP display. If <paramref name="animate"/> is true, the slider
        /// smoothly interpolates to the new value over <see cref="HP_ANIM_DURATION"/> seconds.
        /// The numeric text always updates instantly for accuracy.
        /// </summary>
        /// <param name="currentHp">New current HP value.</param>
        /// <param name="maxHp">Maximum HP (used to normalize the slider).</param>
        /// <param name="animate">Whether to smoothly interpolate the bar. Default: true.</param>
        public void UpdateHP(int currentHp, int maxHp, bool animate = true)
        {
            // Numeric text is always immediate — the player reads numbers first.
            hpNumericText.text = FormatHP(currentHp, maxHp);

            float targetValue = NormalizeHP(currentHp, maxHp);

            if (animate)
            {
                if (_hpAnimRoutine != null)
                    StopCoroutine(_hpAnimRoutine);

                _hpAnimRoutine = StartCoroutine(AnimateSlider(hpSlider.value, targetValue));
            }
            else
            {
                if (_hpAnimRoutine != null)
                {
                    StopCoroutine(_hpAnimRoutine);
                    _hpAnimRoutine = null;
                }

                hpSlider.value = targetValue;
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

                // SmoothStep eases in/out for a polished feel.
                float t = Mathf.SmoothStep(0f, 1f, elapsed / HP_ANIM_DURATION);
                hpSlider.value = Mathf.Lerp(fromValue, toValue, t);

                yield return null;
            }

            // Guarantee exact target value on completion.
            hpSlider.value = toValue;
            _hpAnimRoutine = null;
        }

        // ─────────────────────────────────────────
        //  Helpers
        // ─────────────────────────────────────────

        /// <summary>
        /// Normalizes an HP value to the [0,1] range expected by <see cref="Slider"/>.
        /// Guards against maxHp == 0 to prevent NaN.
        /// </summary>
        private static float NormalizeHP(int currentHp, int maxHp)
        {
            if (maxHp <= 0) return 0f;
            return Mathf.Clamp01((float)currentHp / maxHp);
        }

        /// <summary>
        /// Returns a display string in "current / max" format.
        /// </summary>
        private static string FormatHP(int currentHp, int maxHp)
            => $"{Mathf.Max(0, currentHp)} / {Mathf.Max(0, maxHp)}";
    }
}
