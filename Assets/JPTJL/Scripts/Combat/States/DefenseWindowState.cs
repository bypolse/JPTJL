using UnityEngine;
using JPTJL.FSM;
using JPTJL.Timing;

namespace JPTJL.Combat.States
{
    /// <summary>
    /// Active high-precision window for defensive reactions (Parry / Dodge).
    /// </summary>
    public class DefenseWindowState : IState
    {
        private readonly CombatController controller;
        private bool isResolved;
        private DefenseType chosenDefense;

        public DefenseWindowState(CombatController controller)
        {
            this.controller = controller;
        }

        public void Enter()
        {
            isResolved = false;
            chosenDefense = DefenseType.None;

            CombatEvents.TriggerPhaseChanged(CombatPhase.DefenseWindow);

            // Defensive window configuration: 0.85s total duration, target attack collision at 0.50s, 200ms total parry window (0.10s perfect threshold)
            TimingWindowConfig defenseConfig = new TimingWindowConfig(0.85f, 0.50f, 0.10f, 0.20f, false);
            controller.ActiveDefenseConfig = defenseConfig;

            CombatEvents.TriggerDefenseWindowStarted(DefenseType.None, defenseConfig);

            controller.TimingEngine.StartWindow(defenseConfig, OnDefenseWindowExpired);
        }

        public void OnPlayerDefenseInput(DefenseType type)
        {
            if (isResolved || type == DefenseType.None) return;

            chosenDefense = type;
            controller.ActiveDefenseType = type;

            TimingResult result = controller.TimingEngine.TriggerInput();
            ResolveDefense(result);
        }

        public void Update(float deltaTime)
        {
            if (isResolved) return;

            controller.TimingEngine.Tick(deltaTime);

            if (controller.TimingEngine.IsActive)
            {
                CombatEvents.TriggerDefenseWindowTicked(
                    chosenDefense,
                    controller.TimingEngine.NormalizedProgress,
                    controller.TimingEngine.ElapsedTime
                );
            }
        }

        private void OnDefenseWindowExpired(TimingResult result)
        {
            if (isResolved) return;
            ResolveDefense(result);
        }

        private void ResolveDefense(TimingResult result)
        {
            isResolved = true;
            controller.DefensiveTimingResult = result;

            CombatEvents.TriggerDefenseWindowResolved(chosenDefense, result);
            controller.StateMachine.ChangeState(controller.DamageResolutionState);
        }

        public void Exit()
        {
            controller.TimingEngine.Cancel();
        }
    }
}
