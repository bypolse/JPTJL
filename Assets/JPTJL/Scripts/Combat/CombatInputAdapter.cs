using UnityEngine;
using JPTJL.Damage;
using JPTJL.Data;
using JPTJL.Timing;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace JPTJL.Combat
{
    /// <summary>
    /// Decoupled input and debug bridge.
    /// Demonstrates how the UI/Input layer talks to CombatController purely through public methods
    /// and listens to state changes strictly via CombatEvents.
    /// Supports Unity New Input System.
    /// </summary>
    public class CombatInputAdapter : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private CombatController combatController;
        [SerializeField] private SkillData[] playerSkills;

        private void OnEnable()
        {
            CombatEvents.OnPhaseChanged += HandlePhaseChanged;
            CombatEvents.OnQTEStarted += HandleQTEStarted;
            CombatEvents.OnQTEResolved += HandleQTEResolved;
            CombatEvents.OnDefenseWindowStarted += HandleDefenseWindowStarted;
            CombatEvents.OnDefenseWindowResolved += HandleDefenseWindowResolved;
            CombatEvents.OnDamageResolved += HandleDamageResolved;
            CombatEvents.OnBattleFinished += HandleBattleFinished;
        }

        private void OnDisable()
        {
            CombatEvents.OnPhaseChanged -= HandlePhaseChanged;
            CombatEvents.OnQTEStarted -= HandleQTEStarted;
            CombatEvents.OnQTEResolved -= HandleQTEResolved;
            CombatEvents.OnDefenseWindowStarted -= HandleDefenseWindowStarted;
            CombatEvents.OnDefenseWindowResolved -= HandleDefenseWindowResolved;
            CombatEvents.OnDamageResolved -= HandleDamageResolved;
            CombatEvents.OnBattleFinished -= HandleBattleFinished;
        }

        private void Update()
        {
            if (combatController == null) return;

#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb == null) return;

            // Action Selection Input (Keys 1, 2)
            if (combatController.StateMachine?.CurrentState is States.ActionSelectionState)
            {
                if (kb.digit1Key.wasPressedThisFrame && playerSkills != null && playerSkills.Length > 0)
                {
                    combatController.SelectSkill(playerSkills[0]);
                }
                else if (kb.digit2Key.wasPressedThisFrame && playerSkills != null && playerSkills.Length > 1)
                {
                    combatController.SelectSkill(playerSkills[1]);
                }
            }

            // Offensive QTE Input (Spacebar)
            if (combatController.StateMachine?.CurrentState is States.QTEExecutionState)
            {
                if (kb.spaceKey.wasPressedThisFrame)
                {
                    combatController.TriggerQTEInput();
                }
            }

            // Defensive Window Input (Z = Parry, X = Dodge)
            if (combatController.StateMachine?.CurrentState is States.DefenseWindowState)
            {
                if (kb.zKey.wasPressedThisFrame)
                {
                    combatController.TriggerDefenseInput(DefenseType.Parry);
                }
                else if (kb.xKey.wasPressedThisFrame)
                {
                    combatController.TriggerDefenseInput(DefenseType.Dodge);
                }
            }
#else
            // Legacy input fallback
            if (combatController.StateMachine?.CurrentState is States.ActionSelectionState)
            {
                if (Input.GetKeyDown(KeyCode.Alpha1) && playerSkills != null && playerSkills.Length > 0)
                {
                    combatController.SelectSkill(playerSkills[0]);
                }
                else if (Input.GetKeyDown(KeyCode.Alpha2) && playerSkills != null && playerSkills.Length > 1)
                {
                    combatController.SelectSkill(playerSkills[1]);
                }
            }

            if (combatController.StateMachine?.CurrentState is States.QTEExecutionState)
            {
                if (Input.GetKeyDown(KeyCode.Space))
                {
                    combatController.TriggerQTEInput();
                }
            }

            if (combatController.StateMachine?.CurrentState is States.DefenseWindowState)
            {
                if (Input.GetKeyDown(KeyCode.Z))
                {
                    combatController.TriggerDefenseInput(DefenseType.Parry);
                }
                else if (Input.GetKeyDown(KeyCode.X))
                {
                    combatController.TriggerDefenseInput(DefenseType.Dodge);
                }
            }
#endif
        }

        private void HandlePhaseChanged(CombatPhase phase)
        {
            Debug.Log($"<color=cyan>[Combat Phase]</color> Entering: <b>{phase}</b>");
        }

        private void HandleQTEStarted(TimingWindowConfig config)
        {
            Debug.Log($"<color=yellow>[QTE Started]</color> Target: {config.TargetTime:F2}s, Perfect: ±{config.PerfectThreshold:F3}s, Good: ±{config.GoodThreshold:F3}s. Press [SPACE]!");
        }

        private void HandleQTEResolved(TimingResult result)
        {
            Debug.Log($"<color=yellow>[QTE Result]</color> <b>{result}</b>!");
        }

        private void HandleDefenseWindowStarted(DefenseType defense, TimingWindowConfig config)
        {
            Debug.Log($"<color=orange>[Incoming Attack!]</color> Defense Window open. Press [Z] for Parry or [X] for Dodge!");
        }

        private void HandleDefenseWindowResolved(DefenseType defense, TimingResult result)
        {
            Debug.Log($"<color=orange>[Defense Result]</color> Action: {defense} | Timing: <b>{result}</b>");
        }

        private void HandleDamageResolved(CombatParticipant attacker, CombatParticipant defender, DamageResult result)
        {
            string critText = result.IsCritical ? " <color=red>[CRIT!]</color>" : "";
            string parryText = result.WasParried ? $" <color=green>[PARRIED - Counter {result.CounterDamage} dmg!]</color>" : "";
            string dodgeText = result.WasDodged ? " <color=cyan>[DODGED!]</color>" : "";

            Debug.Log($"<color=red>[Damage]</color> {attacker.Name} dealt {result.FinalDamage} damage to {defender.Name} (Raw: {result.RawDamage}){critText}{parryText}{dodgeText}. Remaining HP: {defender.CurrentHealth}/{defender.MaxHealth}");
        }

        private void HandleBattleFinished(bool playerWon)
        {
            string outcome = playerWon ? "<color=green>VICTORY!</color>" : "<color=red>DEFEAT...</color>";
            Debug.Log($"<color=white>================= BATTLE END: {outcome} =================</color>");
        }
    }
}
