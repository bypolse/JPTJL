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

            // 1. Validar maná suficiente antes de ejecutar cualquier habilidad
            if (!controller.Player.HasMana(skill.ManaCost))
            {
                CombatEvents.TriggerManaInsufficient(controller.Player, skill, skill.ManaCost, controller.Player.CurrentMana);
                CombatEvents.TriggerFloatingFeedback("¡MANÁ INSUFICIENTE!", UnityEngine.Color.red);
                UnityEngine.Debug.LogWarning($"<color=yellow>[Maná Insuficiente]</color> {controller.Player.Name} no tiene suficiente maná para {skill.SkillName}. Requiere {skill.ManaCost} MP, Actual: {controller.Player.CurrentMana} MP.");
                return; // Bloquea la acción y permanece en espera
            }

            // 2. Consumir maná requerido
            controller.Player.ConsumeMana(skill.ManaCost);

            controller.CurrentSkill = skill;
            CombatEvents.TriggerSkillSelected(controller.Player, skill);

            // 3. Manejo según el tipo de habilidad
            if (skill.Type == SkillType.Shield)
            {
                // Habilidad 3: "Barrera Escudo" -> Otorga 10 de Escudo/Absorción inmediatamente
                controller.Player.AddShield(skill.ShieldAmount);
                CombatEvents.TriggerFloatingFeedback($"+{skill.ShieldAmount} ESCUDO", UnityEngine.Color.cyan);
                UnityEngine.Debug.Log($"<color=cyan>[Barrera Escudo]</color> {controller.Player.Name} activa {skill.SkillName} y obtiene {skill.ShieldAmount} de escudo. Escudo total: {controller.Player.CurrentShield}");

                // Pasa el turno al enemigo
                controller.StateMachine.ChangeState(controller.EnemyTurnState);
                return;
            }

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
