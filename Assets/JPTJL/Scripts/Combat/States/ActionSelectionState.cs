using JPTJL.Data;
using JPTJL.FSM;

namespace JPTJL.Combat.States
{
    /// <summary>
    /// Waits for player action/skill input. Purely decoupled from UI.
    /// </summary>
    public class ActionSelectionState : IState
    {
        private readonly CombatController controller;

        public ActionSelectionState(CombatController controller)
        {
            this.controller = controller;
        }

        public void Enter()
        {
            CombatEvents.TriggerPhaseChanged(CombatPhase.ActionSelection);
        }

        public void OnSkillChosen(SkillData skill)
        {
            if (skill == null) return;

            controller.CurrentSkill = skill;
            CombatEvents.TriggerSkillSelected(controller.Player, skill);

            if (skill.RequiresQTE)
            {
                controller.StateMachine.ChangeState(controller.QTEExecutionState);
            }
            else
            {
                controller.OffensiveTimingResult = TimingResult.Good;
                controller.StateMachine.ChangeState(controller.DamageResolutionState);
            }
        }

        public void Update(float deltaTime)
        {
        }

        public void Exit()
        {
        }
    }
}
