using System.Collections.Generic;
using UnityEngine;
using JPTJL.FSM;
using JPTJL.Timing;

namespace JPTJL.Combat.States
{
    /// <summary>
    /// Executes the active offensive Quick Time Event (QTE).
    /// Uses KeyQTEEngine to prompt random keys from [W, A, S, D, Z, X] with a visible timer (e.g. 1.2s),
    /// supporting single-hit and multi-hit abilities (e.g. Doble Estocada).
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

            int hits = controller.CurrentSkill != null ? controller.CurrentSkill.HitCount : 1;
            float duration = controller.CurrentSkill != null ? controller.CurrentSkill.QteDuration : 1.2f;

            controller.KeyQTEEngine.StartSequence(hits, duration, OnQTECompleted);
        }

        /// <summary>
        /// Called when the player presses any of the candidate keys (W, A, S, D, Z, X).
        /// </summary>
        public void OnPlayerKeyInput(KeyCode key)
        {
            if (isResolved || !controller.KeyQTEEngine.IsActive) return;
            controller.KeyQTEEngine.SubmitKeyInput(key);
        }

        /// <summary>
        /// Fallback method for spacebar or generic input trigger.
        /// </summary>
        public void OnPlayerQTEInput()
        {
            if (isResolved || !controller.KeyQTEEngine.IsActive) return;
            // Evaluates with current expected key if generic input used
            controller.KeyQTEEngine.SubmitKeyInput(controller.KeyQTEEngine.CurrentTargetKey);
        }

        public void Update(float deltaTime)
        {
            if (isResolved) return;
            controller.KeyQTEEngine.Tick(deltaTime);
        }

        private void OnQTECompleted(List<QTEHitResult> results)
        {
            isResolved = true;
            controller.LastQTEResults = results;

            // Overall result calculation
            int successCount = 0;
            for (int i = 0; i < results.Count; i++)
            {
                if (results[i].IsSuccess) successCount++;
            }

            if (successCount == results.Count)
            {
                controller.OffensiveTimingResult = TimingResult.Perfect;
            }
            else if (successCount > 0)
            {
                controller.OffensiveTimingResult = TimingResult.Good;
            }
            else
            {
                controller.OffensiveTimingResult = TimingResult.Miss;
            }

            CombatEvents.TriggerQTEResolved(controller.OffensiveTimingResult);
            controller.StateMachine.ChangeState(controller.DamageResolutionState);
        }

        public void Exit()
        {
            controller.KeyQTEEngine.Cancel();
        }
    }
}
