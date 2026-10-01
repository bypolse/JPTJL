using System;
using JPTJL.Combat;

namespace JPTJL.Timing
{
    /// <summary>
    /// Contract for decoupled millisecond-precision timing calculations.
    /// </summary>
    public interface ITimingEngine
    {
        bool IsActive { get; }
        TimingWindowConfig CurrentConfig { get; }
        float ElapsedTime { get; }
        float NormalizedProgress { get; }

        event Action<TimingWindowConfig> OnWindowStarted;
        event Action<float, float> OnWindowTick;
        event Action<TimingResult> OnWindowResolved;

        void StartWindow(TimingWindowConfig config, Action<TimingResult> onResolved = null);
        TimingResult TriggerInput();
        void Cancel();
        void Tick(float deltaTime);
    }
}
