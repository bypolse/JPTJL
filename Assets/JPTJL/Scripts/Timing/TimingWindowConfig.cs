using System;
using UnityEngine;
using JPTJL.Combat;

namespace JPTJL.Timing
{
    /// <summary>
    /// Configuration data for a high-precision deterministic timing window (QTE or Parry/Dodge).
    /// Precision is calculated in fractions of a second (milliseconds).
    /// </summary>
    [Serializable]
    public struct TimingWindowConfig
    {
        [Tooltip("Total duration in seconds before the window times out.")]
        [SerializeField] private float totalDuration;

        [Tooltip("Target timestamp (in seconds from start) representing the optimal hit point.")]
        [SerializeField] private float targetTime;

        [Tooltip("Half-window tolerance in seconds for a Perfect result (e.g. 0.05s = 50ms).")]
        [SerializeField] private float perfectThreshold;

        [Tooltip("Half-window tolerance in seconds for a Good result (e.g. 0.15s = 150ms).")]
        [SerializeField] private float goodThreshold;

        [Tooltip("If true, timing calculations ignore Time.timeScale.")]
        [SerializeField] private bool useUnscaledTime;

        public float TotalDuration => totalDuration;
        public float TargetTime => targetTime;
        public float PerfectThreshold => perfectThreshold;
        public float GoodThreshold => goodThreshold;
        public bool UseUnscaledTime => useUnscaledTime;

        public TimingWindowConfig(float totalDuration, float targetTime, float perfectThreshold, float goodThreshold, bool useUnscaledTime = false)
        {
            this.totalDuration = Mathf.Max(0.01f, totalDuration);
            this.targetTime = Mathf.Clamp(targetTime, 0f, totalDuration);
            this.perfectThreshold = Mathf.Max(0.001f, perfectThreshold);
            this.goodThreshold = Mathf.Max(this.perfectThreshold, goodThreshold);
            this.useUnscaledTime = useUnscaledTime;
        }

        /// <summary>
        /// Pure deterministic evaluation of an elapsed timestamp against the configured thresholds.
        /// </summary>
        public TimingResult Evaluate(float elapsedTime)
        {
            float offset = Mathf.Abs(elapsedTime - targetTime);

            if (offset <= perfectThreshold)
            {
                return TimingResult.Perfect;
            }

            if (offset <= goodThreshold)
            {
                return TimingResult.Good;
            }

            return TimingResult.Miss;
        }
    }
}
