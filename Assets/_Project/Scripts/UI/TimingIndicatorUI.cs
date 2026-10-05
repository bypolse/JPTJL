using System.Collections;
using Project.Combat.Data;
using Project.Combat.Events;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Project.Combat.UI
{
    public enum TimingResult { None, Miss, Good, Perfect }

    public class TimingIndicatorUI : MonoBehaviour
    {
        [Header("Visual References")]
        [SerializeField] private GameObject visualRoot;
        [SerializeField] private RectTransform targetRing;
        [SerializeField] private RectTransform shrinkingRing;
        [SerializeField] private Image shrinkingImage;
        [SerializeField] private TextMeshProUGUI feedbackText;

        [Header("Zone Colors")]
        [SerializeField] private Color normalColor  = Color.white;
        [SerializeField] private Color goodColor    = Color.yellow;
        [SerializeField] private Color perfectColor = Color.green;
        [SerializeField] private Color missColor    = Color.red;

        private static readonly Vector3 StartScale = new Vector3(2.5f, 2.5f, 1f);
        private static readonly Vector3 EndScale   = Vector3.one;

        private Coroutine _shrinkRoutine;
        private Coroutine _feedbackRoutine;

        private float _goodStart;
        private float _goodEnd;
        private float _perfectStart;
        private float _perfectEnd;
        private float _currentNormalizedTime;
        private bool _isActive;

        public bool IsActive => _isActive;

        // ─────────────────────────────────────────
        //  Unity Lifecycle
        // ─────────────────────────────────────────

        private void Awake()
        {
            if (visualRoot != null)
                visualRoot.SetActive(true);

            if (targetRing != null)
                targetRing.gameObject.SetActive(true);

            if (shrinkingRing != null)
                shrinkingRing.gameObject.SetActive(false);

            if (feedbackText != null)
                feedbackText.gameObject.SetActive(false);
        }

        private void OnEnable()
        {
            CombatEvents.OnQTETriggered += HandleQTETriggered;
        }

        private void OnDisable()
        {
            CombatEvents.OnQTETriggered -= HandleQTETriggered;
        }

        // ─────────────────────────────────────────
        //  Event Bus Handlers
        // ─────────────────────────────────────────

        private void HandleQTETriggered(SkillData skill)
        {
            if (skill == null) return;

            ShowIndicator(
                skill.duration,
                skill.goodWindowStart,
                skill.goodWindowEnd,
                skill.perfectWindowStart,
                skill.perfectWindowEnd);
        }

        // ─────────────────────────────────────────
        //  Public API
        // ─────────────────────────────────────────

        public void ShowIndicator(
            float duration,
            float goodStart, float goodEnd,
            float perfectStart, float perfectEnd)
        {
            StopAllActiveCoroutines();

            _goodStart    = goodStart;
            _goodEnd      = goodEnd;
            _perfectStart = perfectStart;
            _perfectEnd   = perfectEnd;

            _currentNormalizedTime = 0f;
            _isActive = true;

            // Ensure the main container and all child visual elements are active
            if (visualRoot != null)
                visualRoot.SetActive(true);

            if (targetRing != null)
                targetRing.gameObject.SetActive(true);

            if (shrinkingRing != null)
            {
                shrinkingRing.gameObject.SetActive(true);
                shrinkingRing.localScale = StartScale;
            }

            if (shrinkingImage != null)
                shrinkingImage.color = normalColor;

            if (feedbackText != null)
                feedbackText.gameObject.SetActive(false);

            _shrinkRoutine = StartCoroutine(
                ShrinkRoutine(duration, goodStart, goodEnd, perfectStart, perfectEnd));
        }

        public TimingResult TryHit()
        {
            if (!_isActive)
                return TimingResult.None;

            if (_shrinkRoutine != null)
            {
                StopCoroutine(_shrinkRoutine);
                _shrinkRoutine = null;
            }

            _isActive = false;

            if (shrinkingRing != null)
                shrinkingRing.gameObject.SetActive(false);

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

            CombatEvents.CompleteQTE(result);
            return result;
        }

        public void HideIndicator()
        {
            _isActive = false;
            StopAllActiveCoroutines();

            if (shrinkingRing != null)
                shrinkingRing.gameObject.SetActive(false);

            if (feedbackText != null)
                feedbackText.gameObject.SetActive(false);

            // Keep visualRoot active if it hosts this component or the targetRing frame
            if (visualRoot != null && visualRoot != gameObject)
                visualRoot.SetActive(false);
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
                _currentNormalizedTime = Mathf.Clamp01(elapsed / duration);

                if (shrinkingRing != null)
                    shrinkingRing.localScale = Vector3.Lerp(StartScale, EndScale, _currentNormalizedTime);

                if (shrinkingImage != null)
                {
                    if (_currentNormalizedTime >= perfectStart && _currentNormalizedTime <= perfectEnd)
                        shrinkingImage.color = perfectColor;
                    else if (_currentNormalizedTime >= goodStart && _currentNormalizedTime <= goodEnd)
                        shrinkingImage.color = goodColor;
                    else
                        shrinkingImage.color = normalColor;
                }

                yield return null;
            }

            // Timeout / Miss
            _isActive = false;
            _shrinkRoutine = null;

            if (shrinkingRing != null)
                shrinkingRing.gameObject.SetActive(false);

            DisplayFeedback("MISS", missColor);
            CombatEvents.CompleteQTE(TimingResult.Miss);
        }

        private void DisplayFeedback(string text, Color color)
        {
            if (_feedbackRoutine != null)
                StopCoroutine(_feedbackRoutine);

            _feedbackRoutine = StartCoroutine(FeedbackRoutine(text, color));
        }

        private IEnumerator FeedbackRoutine(string text, Color color)
        {
            if (feedbackText != null)
            {
                feedbackText.text = text;
                feedbackText.color = color;
                feedbackText.gameObject.SetActive(true);
            }

            yield return new WaitForSeconds(0.6f);

            if (feedbackText != null)
                feedbackText.gameObject.SetActive(false);

            if (shrinkingRing != null)
                shrinkingRing.gameObject.SetActive(false);

            // NOTE: visualRoot is NOT deactivated here so the central target frame
            // and the component's event listeners remain active and reactive for next attempts.
            _feedbackRoutine = null;
        }

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
