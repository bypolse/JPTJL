using UnityEngine;
using JPTJL.FSM;

namespace JPTJL.Combat.States
{
    /// <summary>
    /// Executes AI enemy turn logic. Selects action and opens defensive window for the player.
    /// </summary>
    public class EnemyTurnState : IState
    {
        private readonly CombatController controller;
        private float aiThinkTimer;
        private const float AiThinkDuration = 0.5f;

        public EnemyTurnState(CombatController controller)
        {
            this.controller = controller;
        }

        public void Enter()
        {
            controller.IsPlayerTurn = false;
            controller.ResetTurnContext();
            aiThinkTimer = 0f;

            CombatEvents.TriggerPhaseChanged(CombatPhase.EnemyTurn);
            CombatEvents.TriggerTurnStarted(controller.Enemy);
        }

        public void Update(float deltaTime)
        {
            aiThinkTimer += deltaTime;
            if (aiThinkTimer >= AiThinkDuration)
            {
                // Select enemy skill
                controller.CurrentSkill = controller.EnemyDefaultSkill;
                controller.OffensiveTimingResult = TimingResult.Good; // Enemy baseline execution

                CombatEvents.TriggerSkillSelected(controller.Enemy, controller.CurrentSkill);

                // Open defensive window for player
                controller.StateMachine.ChangeState(controller.DefenseWindowState);
            }
        }

        public void Exit()
        {
        }
    }
}
