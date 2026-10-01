using JPTJL.FSM;

namespace JPTJL.Combat.States
{
    /// <summary>
    /// Player turn beginning state. Sets turn context and prepares for action selection.
    /// </summary>
    public class PlayerTurnState : IState
    {
        private readonly CombatController controller;

        public PlayerTurnState(CombatController controller)
        {
            this.controller = controller;
        }

        public void Enter()
        {
            controller.IsPlayerTurn = true;
            controller.ResetTurnContext();

            CombatEvents.TriggerPhaseChanged(CombatPhase.PlayerTurn);
            CombatEvents.TriggerTurnStarted(controller.Player);

            controller.StateMachine.ChangeState(controller.ActionSelectionState);
        }

        public void Update(float deltaTime)
        {
        }

        public void Exit()
        {
        }
    }
}
