using UnityEngine;
using JPTJL.Damage;
using JPTJL.FSM;

namespace JPTJL.Combat.States
{
    /// <summary>
    /// Calculates and applies damage formulas, counter-attacks, and checks victory/defeat conditions.
    /// </summary>
    public class DamageResolutionState : IState
    {
        private readonly CombatController controller;
        private float delayTimer;
        private const float ResolutionDelay = 0.6f;

        public DamageResolutionState(CombatController controller)
        {
            this.controller = controller;
        }

        public void Enter()
        {
            delayTimer = 0f;
            CombatEvents.TriggerPhaseChanged(CombatPhase.DamageResolution);

            CombatParticipant attacker = controller.IsPlayerTurn ? controller.Player : controller.Enemy;
            CombatParticipant defender = controller.IsPlayerTurn ? controller.Enemy : controller.Player;

            DefenseType defenseType = controller.IsPlayerTurn ? DefenseType.None : controller.ActiveDefenseType;
            TimingResult defenseTiming = controller.IsPlayerTurn ? TimingResult.Miss : controller.DefensiveTimingResult;

            DamageResult damageResult = DamageCalculator.Calculate(
                attacker: attacker.StatsData,
                defender: defender.StatsData,
                skill: controller.CurrentSkill,
                attackTiming: controller.OffensiveTimingResult,
                defenseAction: defenseType,
                defenseTiming: defenseTiming
            );

            // Apply direct damage to defender
            defender.TakeDamage(damageResult.FinalDamage);

            // Apply counter damage to attacker if a perfect parry was achieved
            if (damageResult.CounterDamage > 0)
            {
                attacker.TakeDamage(damageResult.CounterDamage);
            }

            CombatEvents.TriggerDamageResolved(attacker, defender, damageResult);
        }

        public void Update(float deltaTime)
        {
            delayTimer += deltaTime;
            if (delayTimer < ResolutionDelay) return;

            // Check if battle should end
            if (!controller.Player.IsAlive)
            {
                controller.BattleWon = false;
                controller.StateMachine.ChangeState(controller.BattleEndState);
                return;
            }

            if (!controller.Enemy.IsAlive)
            {
                controller.BattleWon = true;
                controller.StateMachine.ChangeState(controller.BattleEndState);
                return;
            }

            // Otherwise, alternate turn
            if (controller.IsPlayerTurn)
            {
                controller.StateMachine.ChangeState(controller.EnemyTurnState);
            }
            else
            {
                controller.StateMachine.ChangeState(controller.PlayerTurnState);
            }
        }

        public void Exit()
        {
        }
    }
}
