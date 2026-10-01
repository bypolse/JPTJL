using System;
using UnityEngine;
using JPTJL.Combat;

namespace JPTJL.Timing
{
    /// <summary>
    /// Pure C# high-precision timing engine.
    /// Accumulates deltas deterministically and evaluates QTE or defense input.
    /// </summary>
    public class TimingEngine : ITimingEngine
    {
        private TimingWindowConfig currentConfig;
        private float elapsedTime;
        private bool isActive;
        private Action<TimingResult> resolutionCallback;

        public bool IsActive => isActive;
        public TimingWindowConfig CurrentConfig => currentConfig;
        public float ElapsedTime => elapsedTime;
        public float NormalizedProgress => currentConfig.TotalDuration > 0f ? Mathf.Clamp01(elapsedTime / currentConfig.TotalDuration) : 0f;

        public event Action<TimingWindowConfig> OnWindowStarted;
        public event Action<float, float> OnWindowTick;
        public event Action<TimingResult> OnWindowResolved;

        public void StartWindow(TimingWindowConfig config, Action<TimingResult> onResolved = null)
        {
            currentConfig = config;
            elapsedTime = 0f;
            isActive = true;
            resolutionCallback = onResolved;

            OnWindowStarted?.Invoke(currentConfig);
        }

        public void Tick(float deltaTime)
        {
            if (!isActive) return;

            elapsedTime += Mathf.Max(0f, deltaTime);
            OnWindowTick?.Invoke(NormalizedProgress, elapsedTime);

            // Check for window expiration (automatic Miss)
            if (elapsedTime >= currentConfig.TotalDuration)
            {
                Resolve(TimingResult.Miss);
            }
        }

        public TimingResult TriggerInput()
        {
            if (!isActive) return TimingResult.Miss;

            TimingResult result = currentConfig.Evaluate(elapsedTime);
            Resolve(result);
            return result;
        }

        public void Cancel()
        {
            if (!isActive) return;
            isActive = false;
            resolutionCallback = null;
        }

        private void Resolve(TimingResult result)
        {
            if (!isActive) return;

            isActive = false;
            var callback = resolutionCallback;
            resolutionCallback = null;

            OnWindowResolved?.Invoke(result);
            callback?.Invoke(result);
        }
    }
}
