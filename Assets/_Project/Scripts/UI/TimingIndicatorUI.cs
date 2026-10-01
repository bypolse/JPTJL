using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Project.Combat.UI
{
    /// <summary>
    /// Visualizes the attack QTE as a shrinking ring that closes toward a static
    /// target ring. Color tints the ring based on current timing zone.
    /// This component is purely presentational: it reads data passed via public
    /// methods and never queries managers or modifies game state.
    /// </summary>
    public class TimingIndicatorUI : MonoBehaviour
    {
        // ─────────────────────────────────────────
        //  Serialized References
        // ─────────────────────────────────────────

        [Header("Visual References")]
        [Tooltip("Root container — toggled to show/hide the entire indicator.")]
        [SerializeField] private GameObject visualRoot;

        [Tooltip("The static inner ring that marks the target zone.")]
        [SerializeField] private RectTransform targetRing;

        [Tooltip("The ring that shrinks toward the target ring over time.")]
        [SerializeField] private RectTransform shrinkingRing;

        [Tooltip("Image component on the shrinking ring, used for color feedback.")]
        [SerializeField] private Image shrinkingImage;

        [Tooltip("Floating label shown briefly after input (or on auto-Miss).")]
        [SerializeField] private TextMeshProUGUI feedbackText;

        // ─────────────────────────────────────────
        //  Color Configuration
        // ─────────────────────────────────────────

        [Header("Zone Colors")]
        [Tooltip("Ring color when outside all timing windows.")]
        [SerializeField] private Color normalColor  = Color.white;

        [Tooltip("Ring color when inside the Good timing window.")]
        [SerializeField] private Color goodColor    = Color.yellow;

        [Tooltip("Ring color when inside the Perfect timing window.")]
        [SerializeField] private Color perfectColor = Color.green;

        [Tooltip("Color of the feedback text on a Miss.")]
        [SerializeField] private Color missColor    = Color.red;

        // ─────────────────────────────────────────
        //  Private State
        // ─────────────────────────────────────────

        private static readonly Vector3 StartScale = new Vector3(2.5f, 2.5f, 1f);
        private static readonly Vector3 EndScale   = Vector3.one;

        private Coroutine _shrinkRoutine;
        private Coroutine _feedbackRoutine;

        // ─────────────────────────────────────────
        //  Public API
        // ─────────────────────────────────────────

        /// <summary>
        /// Activates the indicator and begins shrinking the ring toward the target.
        /// Tints the ring according to which timing zone the current progress is in.
        /// If no input interrupts before <paramref name="duration"/> elapses, shows MISS.
        /// </summary>
        /// <param name="duration">Total time in seconds for the ring to close.</param>
        /// <param name="goodStart">Normalized start of the Good window [0,1].</param>
        /// <param name="goodEnd">Normalized end of the Good window [0,1].</param>
        /// <param name="perfectStart">Normalized start of the Perfect window [0,1].</param>
        /// <param name="perfectEnd">Normalized end of the Perfect window [0,1].</param>
        public void ShowIndicator(
            float duration,
            float goodStart, float goodEnd,
            float perfectStart, float perfectEnd)
        {
            StopAllActiveCoroutines();

            visualRoot.SetActive(true);
            feedbackText.gameObject.SetActive(false);
            shrinkingRing.localScale = StartScale;

            _shrinkRoutine = StartCoroutine(
                ShrinkRoutine(duration, goodStart, goodEnd, perfectStart, perfectEnd));
        }

        /// <summary>
        /// Immediately stops the shrink coroutine and hides the indicator.
        /// Call this when the player provides input before time runs out.
        /// </summary>
        public void HideIndicator()
        {
            StopAllActiveCoroutines();
            visualRoot.SetActive(false);
        }

        /// <summary>
        /// Displays a result label (e.g. "PERFECT", "GOOD", "MISS") for 0.8 seconds,
        /// then hides it. Can be called independently of the shrink routine.
        /// </summary>
        /// <param name="result">Text to display.</param>
        /// <param name="resultColor">Color of the displayed text.</param>
        public void DisplayFeedback(string result, Color resultColor)
        {
            if (_feedbackRoutine != null)
                StopCoroutine(_feedbackRoutine);

            _feedbackRoutine = StartCoroutine(FeedbackRoutine(result, resultColor));
        }

        // ─────────────────────────────────────────
        //  Coroutines
        // ─────────────────────────────────────────

        private IEnumerator ShrinkRoutine(
            float duration,
            float goodStart, float goodEnd,
            float perfectStart, float perfectEnd)
        {
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float normalizedTime = Mathf.Clamp01(elapsed / duration);

                // Interpolate scale from start to target.
                shrinkingRing.localScale = Vector3.Lerp(StartScale, EndScale, normalizedTime);

                // Tint ring based on current zone (Perfect checked first — it's the inner zone).
                if (normalizedTime >= perfectStart && normalizedTime <= perfectEnd)
                    shrinkingImage.color = perfectColor;
                else if (normalizedTime >= goodStart && normalizedTime <= goodEnd)
                    shrinkingImage.color = goodColor;
                else
                    shrinkingImage.color = normalColor;

                yield return null;
            }

            // Duration elapsed with no player input → auto Miss.
            shrinkingRing.localScale = EndScale;
            DisplayFeedback("MISS", missColor);
            visualRoot.SetActive(false);

            _shrinkRoutine = null;
        }

        private IEnumerator FeedbackRoutine(string result, Color resultColor)
        {
            feedbackText.text  = result;
            feedbackText.color = resultColor;
            feedbackText.gameObject.SetActive(true);

            yield return new WaitForSeconds(0.8f);

            feedbackText.gameObject.SetActive(false);
            _feedbackRoutine = null;
        }

        // ─────────────────────────────────────────
        //  Helpers
        // ─────────────────────────────────────────

        private void StopAllActiveCoroutines()
        {
            if (_shrinkRoutine != null)
            {
                StopCoroutine(_shrinkRoutine);
                _shrinkRoutine = null;
            }

            if (_feedbackRoutine != null)
            {
                StopCoroutine(_feedbackRoutine);
                _feedbackRoutine = null;
            }
        }
    }
}
