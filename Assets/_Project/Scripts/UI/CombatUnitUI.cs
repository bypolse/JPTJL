using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Project.Combat.UI
{
    /// <summary>
    /// Displays and animates the status bar (Health, Mana, Portrait and Name)
    /// for a combatant (hero or enemy) in accordance with the combat UI layout.
    /// </summary>
    public class CombatUnitUI : MonoBehaviour
    {
        // ─────────────────────────────────────────
        //  Serialized References
        // ─────────────────────────────────────────

        [Header("Unit Info & Portrait")]
        [Tooltip("Label that displays the unit's name.")]
        [SerializeField] private TextMeshProUGUI nameText;

        [Tooltip("Icon or portrait badge for the character.")]
        [SerializeField] private Image portraitImage;

        [Header("HP Bar")]
        [Tooltip("Slider whose value represents current HP as a 0–1 ratio.")]
        [SerializeField] private Slider hpSlider;

        [Tooltip("Label showing current and max HP.")]
        [SerializeField] private TextMeshProUGUI hpNumericText;

        [Header("Mana Bar (Optional for Player)")]
        [Tooltip("Slider whose value represents current Mana as a 0–1 ratio.")]
        [SerializeField] private Slider manaSlider;

        [Tooltip("Label showing current and max Mana.")]
        [SerializeField] private TextMeshProUGUI manaNumericText;

        // ─────────────────────────────────────────
        //  Public Accessors / Setup
        // ─────────────────────────────────────────

        public Image PortraitImage => portraitImage;
        public Slider ManaSlider => manaSlider;

        public void ConfigureReferences(
            TextMeshProUGUI name,
            Image portrait,
            Slider hp,
            TextMeshProUGUI hpNumeric,
            Slider mana = null,
            TextMeshProUGUI manaNumeric = null)
        {
            nameText = name;
            portraitImage = portrait;
            hpSlider = hp;
            hpNumericText = hpNumeric;
            manaSlider = mana;
            manaNumericText = manaNumeric;
        }

        public void SetPortrait(Image portrait)
        {
            portraitImage = portrait;
        }

        public void SetManaBar(Slider mana, TextMeshProUGUI manaNumeric)
        {
            manaSlider = mana;
            manaNumericText = manaNumeric;
        }

        // ─────────────────────────────────────────
        //  Private State
        // ─────────────────────────────────────────

        private const float BAR_ANIM_DURATION = 0.25f;

        private Coroutine _hpAnimRoutine;
        private Coroutine _mpAnimRoutine;

        // ─────────────────────────────────────────
        //  Public API
        // ─────────────────────────────────────────

        /// <summary>
        /// Sets up the unit panel with its initial values.
        /// </summary>
        public void Initialize(string unitName, int currentHp, int maxHp)
        {
            Initialize(unitName, currentHp, maxHp, 0, 0, null);
        }

        /// <summary>
        /// Extended overload including Mana and optional Portrait.
        /// </summary>
        public void Initialize(string unitName, int currentHp, int maxHp, int currentMp, int maxMp, Sprite portrait = null)
        {
            if (nameText != null)
                nameText.text = unitName;

            if (hpSlider != null)
                hpSlider.value = NormalizeValue(currentHp, maxHp);

            if (hpNumericText != null)
                hpNumericText.text = FormatValue("Salud", currentHp, maxHp);

            if (manaSlider != null)
            {
                if (maxMp > 0)
                {
                    manaSlider.gameObject.SetActive(true);
                    manaSlider.value = NormalizeValue(currentMp, maxMp);
                }
                else
                {
                    manaSlider.gameObject.SetActive(false);
                }
            }

            if (manaNumericText != null)
            {
                if (maxMp > 0)
                {
                    manaNumericText.gameObject.SetActive(true);
                    manaNumericText.text = FormatValue("Maná", currentMp, maxMp);
                }
                else
                {
                    manaNumericText.gameObject.SetActive(false);
                }
            }

            if (portrait != null)
            {
                SetPortrait(portrait);
            }
        }

        public void SetPortrait(Sprite portrait)
        {
            if (portraitImage != null)
            {
                portraitImage.sprite = portrait;
                portraitImage.gameObject.SetActive(portrait != null);
            }
        }

        /// <summary>
        /// Updates the HP display with optional smooth transition animation.
        /// </summary>
        public void UpdateHP(int currentHp, int maxHp, bool animate = true)
        {
            if (hpNumericText != null)
                hpNumericText.text = FormatValue("Salud", currentHp, maxHp);

            if (hpSlider == null) return;

            float targetValue = NormalizeValue(currentHp, maxHp);

            if (animate && gameObject.activeInHierarchy)
            {
                if (_hpAnimRoutine != null)
                    StopCoroutine(_hpAnimRoutine);

                _hpAnimRoutine = StartCoroutine(AnimateSlider(hpSlider, hpSlider.value, targetValue, () => _hpAnimRoutine = null));
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

        /// <summary>
        /// Updates the Mana display with optional smooth transition animation.
        /// </summary>
        public void UpdateMana(int currentMp, int maxMp, bool animate = true)
        {
            if (manaNumericText != null)
                manaNumericText.text = FormatValue("Maná", currentMp, maxMp);

            if (manaSlider == null) return;

            float targetValue = NormalizeValue(currentMp, maxMp);

            if (animate && gameObject.activeInHierarchy)
            {
                if (_mpAnimRoutine != null)
                    StopCoroutine(_mpAnimRoutine);

                _mpAnimRoutine = StartCoroutine(AnimateSlider(manaSlider, manaSlider.value, targetValue, () => _mpAnimRoutine = null));
            }
            else
            {
                if (_mpAnimRoutine != null)
                {
                    StopCoroutine(_mpAnimRoutine);
                    _mpAnimRoutine = null;
                }

                manaSlider.value = targetValue;
            }
        }

        // ─────────────────────────────────────────
        //  Coroutines
        // ─────────────────────────────────────────

        private IEnumerator AnimateSlider(Slider slider, float fromValue, float toValue, System.Action onComplete)
        {
            float elapsed = 0f;

            while (elapsed < BAR_ANIM_DURATION)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / BAR_ANIM_DURATION);
                slider.value = Mathf.Lerp(fromValue, toValue, t);
                yield return null;
            }

            slider.value = toValue;
            onComplete?.Invoke();
        }

        // ─────────────────────────────────────────
        //  Helpers
        // ─────────────────────────────────────────

        private static float NormalizeValue(int current, int max)
        {
            if (max <= 0) return 0f;
            return Mathf.Clamp01((float)current / max);
        }

        private static string FormatValue(string label, int current, int max)
            => $"{label} {Mathf.Max(0, current)}/{Mathf.Max(0, max)}";
    }
}
