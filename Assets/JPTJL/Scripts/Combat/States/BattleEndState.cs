using JPTJL.FSM;

namespace JPTJL.Combat.States
{
    /// <summary>
    /// Final combat state triggered upon victory or defeat.
    /// </summary>
    public class BattleEndState : IState
    {
        private readonly CombatController controller;

        public BattleEndState(CombatController controller)
        {
            this.controller = controller;
        }

        public void Enter()
        {
            CombatEvents.TriggerPhaseChanged(CombatPhase.BattleEnd);
            CombatEvents.TriggerBattleFinished(controller.BattleWon);
        }

        public void Update(float deltaTime)
        {
        }

        public void Exit()
        {
        }
    }
}
