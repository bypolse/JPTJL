using System;
using System.Collections.Generic;
using UnityEngine;
using JPTJL.Combat;

namespace JPTJL.Timing
{
    /// <summary>
    /// Represents the evaluation result of a single QTE hit.
    /// </summary>
    [Serializable]
    public struct QTEHitResult
    {
        public int HitIndex;
        public KeyCode ExpectedKey;
        public KeyCode PressedKey;
        public bool IsSuccess;
        public TimingResult Result;
        public float DamageMultiplier;
        public string FeedbackText;

        public QTEHitResult(int index, KeyCode expected, KeyCode pressed, bool success, float multiplier, string feedback)
        {
            HitIndex = index;
            ExpectedKey = expected;
            PressedKey = pressed;
            IsSuccess = success;
            Result = success ? TimingResult.Perfect : TimingResult.Miss;
            DamageMultiplier = multiplier;
            FeedbackText = feedback;
        }
    }

    /// <summary>
    /// Pure timing and key prompt engine for Offensive QTEs.
    /// Randomly selects a key from [W, A, S, D, Z, X], tracks a visible countdown timer (e.g. 1.2s),
    /// applies +20% damage on success ("PERFECT!") and base damage on failure ("NORMAL" / "FALLO").
    /// Supports multi-hit skills (e.g. Doble Estocada) by advancing hits with independent random keys.
    /// </summary>
    public class KeyQTEEngine
    {
        private static readonly KeyCode[] CandidateKeys = new KeyCode[]
        {
            KeyCode.W,
            KeyCode.A,
            KeyCode.S,
            KeyCode.D,
            KeyCode.Z,
            KeyCode.X
        };

        private bool isActive;
        private int totalHits;
        private int currentHitIndex;
        private float durationPerHit;
        private float timeRemaining;
        private KeyCode currentTargetKey;
        private Action<List<QTEHitResult>> sequenceCallback;
        private readonly List<QTEHitResult> hitResults = new List<QTEHitResult>();

        public bool IsActive => isActive;
        public int TotalHits => totalHits;
        public int CurrentHitIndex => currentHitIndex;
        public float DurationPerHit => durationPerHit;
        public float TimeRemaining => timeRemaining;
        public float NormalizedProgress => durationPerHit > 0f ? Mathf.Clamp01(1f - (timeRemaining / durationPerHit)) : 0f;
        public KeyCode CurrentTargetKey => currentTargetKey;
        public string CurrentTargetKeyName => KeyCodeToString(currentTargetKey);
        public IReadOnlyList<QTEHitResult> HitResults => hitResults;

        /// <summary>
        /// Starts a QTE sequence with the specified hit count and countdown duration per hit.
        /// </summary>
        public void StartSequence(int hits, float duration, Action<List<QTEHitResult>> onComplete)
        {
            isActive = true;
            totalHits = Mathf.Max(1, hits);
            currentHitIndex = 0;
            durationPerHit = Mathf.Max(0.2f, duration);
            sequenceCallback = onComplete;
            hitResults.Clear();

            PromptNextKey();
        }

        public void Tick(float deltaTime)
        {
            if (!isActive) return;

            timeRemaining -= Mathf.Max(0f, deltaTime);
            CombatEvents.TriggerKeyQTETicked(timeRemaining, NormalizedProgress);

            if (timeRemaining <= 0f)
            {
                // Timeout failure
                ResolveCurrentHit(KeyCode.None, false, "FALLO");
            }
        }

        /// <summary>
        /// Validates player input key against the active target key.
        /// </summary>
        public void SubmitKeyInput(KeyCode pressedKey)
        {
            if (!isActive) return;

            bool isCorrect = (pressedKey == currentTargetKey);
            string feedback = isCorrect ? "PERFECT!" : "FALLO";
            ResolveCurrentHit(pressedKey, isCorrect, feedback);
        }

        private void ResolveCurrentHit(KeyCode pressedKey, bool success, string feedback)
        {
            if (!isActive) return;

            float multiplier = success ? 1.20f : 1.0f; // +20% extra on success, 1.0x (base) on failure
            TimingResult timingResult = success ? TimingResult.Perfect : TimingResult.Miss;

            QTEHitResult hit = new QTEHitResult(
                index: currentHitIndex,
                expected: currentTargetKey,
                pressed: pressedKey,
                success: success,
                multiplier: multiplier,
                feedback: feedback
            );
            hitResults.Add(hit);

            // Dispatch visual events
            Color feedbackColor = success ? new Color(0.2f, 1f, 0.4f) : new Color(1f, 0.3f, 0.3f);
            CombatEvents.TriggerKeyQTEEvaluated(currentTargetKey, success, timingResult, feedback, currentHitIndex, totalHits);
            CombatEvents.TriggerFloatingFeedback(feedback, feedbackColor);

            if (currentHitIndex + 1 < totalHits)
            {
                // Advance to next hit in multi-hit sequence (e.g. Doble Estocada)
                currentHitIndex++;
                PromptNextKey();
            }
            else
            {
                // Complete sequence
                CompleteSequence();
            }
        }

        private void PromptNextKey()
        {
            // Pick a random key from the allowed candidate pool
            int randomIndex = UnityEngine.Random.Range(0, CandidateKeys.Length);
            currentTargetKey = CandidateKeys[randomIndex];
            timeRemaining = durationPerHit;

            CombatEvents.TriggerKeyQTEPrompt(
                currentTargetKey,
                CurrentTargetKeyName,
                durationPerHit,
                currentHitIndex + 1,
                totalHits
            );
        }

        private void CompleteSequence()
        {
            isActive = false;
            var callback = sequenceCallback;
            sequenceCallback = null;

            callback?.Invoke(new List<QTEHitResult>(hitResults));
        }

        public void Cancel()
        {
            isActive = false;
            sequenceCallback = null;
            hitResults.Clear();
        }

        public static string KeyCodeToString(KeyCode code)
        {
            switch (code)
            {
                case KeyCode.W: return "W";
                case KeyCode.A: return "A";
                case KeyCode.S: return "S";
                case KeyCode.D: return "D";
                case KeyCode.Z: return "Z";
                case KeyCode.X: return "X";
                default: return code.ToString();
            }
        }
    }
}
