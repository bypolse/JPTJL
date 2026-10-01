using UnityEngine;
using JPTJL.FSM;

namespace JPTJL.Combat.States
{
    /// <summary>
    /// Initial battle setup state. Dispatches initialization events and determines turn order.
    /// </summary>
    public class BattleStartState : IState
    {
        private readonly CombatController controller;
        private float introTimer;
        private const float IntroDuration = 0.5f;

        public BattleStartState(CombatController controller)
        {
            this.controller = controller;
        }

        public void Enter()
        {
            introTimer = 0f;
            CombatEvents.TriggerPhaseChanged(CombatPhase.BattleStart);
        }

        public void Update(float deltaTime)
        {
            introTimer += deltaTime;
            if (introTimer >= IntroDuration)
            {
                // Determine turn order based on Speed
                bool playerFirst = controller.Player.StatsData.Speed >= controller.Enemy.StatsData.Speed;
                if (playerFirst)
                {
                    controller.StateMachine.ChangeState(controller.PlayerTurnState);
                }
                else
                {
                    controller.StateMachine.ChangeState(controller.EnemyTurnState);
                }
            }
        }

        public void Exit()
        {
        }
    }
}
