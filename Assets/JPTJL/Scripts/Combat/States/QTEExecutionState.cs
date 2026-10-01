using JPTJL.FSM;
using JPTJL.Timing;

namespace JPTJL.Combat.States
{
    /// <summary>
    /// Executes the active offensive Quick Time Event (QTE).
    /// Precision calculation determines attack multiplier.
    /// </summary>
    public class QTEExecutionState : IState
    {
        private readonly CombatController controller;
        private bool isResolved;

        public QTEExecutionState(CombatController controller)
        {
            this.controller = controller;
        }

        public void Enter()
        {
            isResolved = false;
            CombatEvents.TriggerPhaseChanged(CombatPhase.QTEExecution);

            TimingWindowConfig config = controller.CurrentSkill.TimingConfig;
            CombatEvents.TriggerQTEStarted(config);

            controller.TimingEngine.StartWindow(config, OnQTEResolved);
        }

        public void OnPlayerQTEInput()
        {
            if (isResolved) return;
            controller.TimingEngine.TriggerInput();
        }

        public void Update(float deltaTime)
        {
            if (isResolved) return;

            float dt = controller.CurrentSkill.TimingConfig.UseUnscaledTime ? UnityEngine.Time.unscaledDeltaTime : deltaTime;
            controller.TimingEngine.Tick(dt);

            if (controller.TimingEngine.IsActive)
            {
                CombatEvents.TriggerQTETicked(controller.TimingEngine.NormalizedProgress, controller.TimingEngine.ElapsedTime);
            }
        }

        private void OnQTEResolved(TimingResult result)
        {
            isResolved = true;
            controller.OffensiveTimingResult = result;
            CombatEvents.TriggerQTEResolved(result);

            controller.StateMachine.ChangeState(controller.DamageResolutionState);
        }

        public void Exit()
        {
            controller.TimingEngine.Cancel();
        }
    }
}
