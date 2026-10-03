using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Project.Combat.UI
{
    /// <summary>
    /// Result of a player's timed input against the shrinking ring.
    /// </summary>
    public enum TimingResult { None, Miss, Good, Perfect }

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

        // Cached window values so TryHit() can evaluate without extra parameters.
        private float _goodStart;
        private float _goodEnd;
        private float _perfectStart;
        private float _perfectEnd;

        // Normalized progress [0,1] written every frame by ShrinkRoutine.
        private float _currentNormalizedTime;

        // True while ShrinkRoutine is running and awaiting player input.
        private bool _isActive;

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

            // Cache windows so TryHit() can evaluate without extra parameters.
            _goodStart    = goodStart;
            _goodEnd      = goodEnd;
            _perfectStart = perfectStart;
            _perfectEnd   = perfectEnd;

            _currentNormalizedTime = 0f;
            _isActive = true;

            visualRoot.SetActive(true);
            feedbackText.gameObject.SetActive(false);
            shrinkingRing.gameObject.SetActive(true);
            shrinkingRing.localScale = StartScale;

            _shrinkRoutine = StartCoroutine(
                ShrinkRoutine(duration, goodStart, goodEnd, perfectStart, perfectEnd));
        }

        /// <summary>
        /// Called when the player presses the action button during an active QTE.
        /// Stops the animation immediately, evaluates the current normalized progress
        /// against the cached timing windows, shows feedback, and returns the result.
        /// Returns <see cref="TimingResult.None"/> if the indicator is not active.
        /// </summary>
        public TimingResult TryHit()
        {
            if (!_isActive)
                return TimingResult.None;

            // Stop the shrink animation — capture progress at this exact frame.
            if (_shrinkRoutine != null)
            {
                StopCoroutine(_shrinkRoutine);
                _shrinkRoutine = null;
            }

            _isActive = false;

            // Evaluate captured progress against cached windows.
            TimingResult result;

            if (_currentNormalizedTime >= _perfectStart && _currentNormalizedTime <= _perfectEnd)
            {
                result = TimingResult.Perfect;
                DisplayFeedback("PERFECT!", perfectColor);
            }
            else if (_currentNormalizedTime >= _goodStart && _currentNormalizedTime <= _goodEnd)
            {
                result = TimingResult.Good;
                DisplayFeedback("GOOD!", goodColor);
            }
            else
            {
                result = TimingResult.Miss;
                DisplayFeedback("MISS", missColor);
            }

            // Hide the ring but keep the feedback label alive.
            shrinkingRing.gameObject.SetActive(false);

            return result;
        }

        /// <summary>
        /// True while the shrink-ring animation is running and awaiting player input.
        /// Read by external callers (e.g. MockCombatTester) to toggle Space behaviour.
        /// </summary>
        public bool IsActive => _isActive;



        /// <summary>
        /// Immediately stops the shrink coroutine and hides the indicator.
        /// Call this when the player provides input before time runs out.
        /// </summary>
        public void HideIndicator()
        {
            _isActive = false;
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

            // Re-enable shrinking ring in case TryHit() hid it in a previous run.
            shrinkingRing.gameObject.SetActive(true);

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                _currentNormalizedTime = Mathf.Clamp01(elapsed / duration);

                // Interpolate scale from start to target.
                shrinkingRing.localScale = Vector3.Lerp(StartScale, EndScale, _currentNormalizedTime);

                // Tint ring based on current zone (Perfect first — it is the inner zone).
                if (_currentNormalizedTime >= perfectStart && _currentNormalizedTime <= perfectEnd)
                    shrinkingImage.color = perfectColor;
                else if (_currentNormalizedTime >= goodStart && _currentNormalizedTime <= goodEnd)
                    shrinkingImage.color = goodColor;
                else
                    shrinkingImage.color = normalColor;

                yield return null;
            }

            // Duration elapsed with no player input → auto Miss.
            _isActive = false;
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
