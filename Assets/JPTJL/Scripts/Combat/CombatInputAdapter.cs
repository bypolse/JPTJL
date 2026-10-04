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
            CombatEvents.OnKeyQTEPrompt += HandleKeyQTEPrompt;
            CombatEvents.OnKeyQTEEvaluated += HandleKeyQTEEvaluated;
            CombatEvents.OnManaInsufficient += HandleManaInsufficient;
            CombatEvents.OnShieldChanged += HandleShieldChanged;
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
            CombatEvents.OnKeyQTEPrompt -= HandleKeyQTEPrompt;
            CombatEvents.OnKeyQTEEvaluated -= HandleKeyQTEEvaluated;
            CombatEvents.OnManaInsufficient -= HandleManaInsufficient;
            CombatEvents.OnShieldChanged -= HandleShieldChanged;
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

            // 1. Selección de Habilidades (Teclas 1, 2, 3, 4)
            if (combatController.StateMachine?.CurrentState is States.ActionSelectionState)
            {
                if (kb.digit1Key.wasPressedThisFrame && HasSkill(0)) combatController.SelectSkill(playerSkills[0]);
                else if (kb.digit2Key.wasPressedThisFrame && HasSkill(1)) combatController.SelectSkill(playerSkills[1]);
                else if (kb.digit3Key.wasPressedThisFrame && HasSkill(2)) combatController.SelectSkill(playerSkills[2]);
                else if (kb.digit4Key.wasPressedThisFrame && HasSkill(3)) combatController.SelectSkill(playerSkills[3]);
            }

            // 2. Teclas de QTE Ofensivo: [W, A, S, D, Z, X]
            if (combatController.StateMachine?.CurrentState is States.QTEExecutionState)
            {
                if (kb.wKey.wasPressedThisFrame) combatController.TriggerKeyQTEInput(KeyCode.W);
                else if (kb.aKey.wasPressedThisFrame) combatController.TriggerKeyQTEInput(KeyCode.A);
                else if (kb.sKey.wasPressedThisFrame) combatController.TriggerKeyQTEInput(KeyCode.S);
                else if (kb.dKey.wasPressedThisFrame) combatController.TriggerKeyQTEInput(KeyCode.D);
                else if (kb.zKey.wasPressedThisFrame) combatController.TriggerKeyQTEInput(KeyCode.Z);
                else if (kb.xKey.wasPressedThisFrame) combatController.TriggerKeyQTEInput(KeyCode.X);
                else if (kb.spaceKey.wasPressedThisFrame) combatController.TriggerQTEInput();
            }

            // 3. Ventana Defensiva en turno enemigo (Z, Espacio o Clic = Parry; X = Dodge)
            if (combatController.StateMachine?.CurrentState is States.DefenseWindowState)
            {
                bool parryPressed = kb.zKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame;
                if (UnityEngine.InputSystem.Mouse.current != null && UnityEngine.InputSystem.Mouse.current.leftButton.wasPressedThisFrame)
                {
                    parryPressed = true;
                }

                if (parryPressed) combatController.TriggerDefenseInput(DefenseType.Parry);
                else if (kb.xKey.wasPressedThisFrame) combatController.TriggerDefenseInput(DefenseType.Dodge);
            }
#else
            // Fallback Sistema de Input Legacy
            if (combatController.StateMachine?.CurrentState is States.ActionSelectionState)
            {
                if (Input.GetKeyDown(KeyCode.Alpha1) && HasSkill(0)) combatController.SelectSkill(playerSkills[0]);
                else if (Input.GetKeyDown(KeyCode.Alpha2) && HasSkill(1)) combatController.SelectSkill(playerSkills[1]);
                else if (Input.GetKeyDown(KeyCode.Alpha3) && HasSkill(2)) combatController.SelectSkill(playerSkills[2]);
                else if (Input.GetKeyDown(KeyCode.Alpha4) && HasSkill(3)) combatController.SelectSkill(playerSkills[3]);
            }

            if (combatController.StateMachine?.CurrentState is States.QTEExecutionState)
            {
                if (Input.GetKeyDown(KeyCode.W)) combatController.TriggerKeyQTEInput(KeyCode.W);
                else if (Input.GetKeyDown(KeyCode.A)) combatController.TriggerKeyQTEInput(KeyCode.A);
                else if (Input.GetKeyDown(KeyCode.S)) combatController.TriggerKeyQTEInput(KeyCode.S);
                else if (Input.GetKeyDown(KeyCode.D)) combatController.TriggerKeyQTEInput(KeyCode.D);
                else if (Input.GetKeyDown(KeyCode.Z)) combatController.TriggerKeyQTEInput(KeyCode.Z);
                else if (Input.GetKeyDown(KeyCode.X)) combatController.TriggerKeyQTEInput(KeyCode.X);
                else if (Input.GetKeyDown(KeyCode.Space)) combatController.TriggerQTEInput();
            }

            if (combatController.StateMachine?.CurrentState is States.DefenseWindowState)
            {
                if (Input.GetKeyDown(KeyCode.Z) || Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0))
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

        private bool HasSkill(int index) => playerSkills != null && index >= 0 && index < playerSkills.Length && playerSkills[index] != null;

        private void HandlePhaseChanged(CombatPhase phase)
        {
            Debug.Log($"<color=cyan>[Fase de Combate]</color> Entrando en: <b>{phase}</b> (Estado: {combatController.CurrentTurnState})");
        }

        private void HandleKeyQTEPrompt(KeyCode key, string keyName, float duration, int hitIndex, int totalHits)
        {
            Debug.Log($"<color=yellow>[QTE Prompt]</color> <b>¡Presiona [{keyName}]!</b> Golpe {hitIndex}/{totalHits} - Tiempo: {duration:F1}s");
        }

        private void HandleKeyQTEEvaluated(KeyCode key, bool success, TimingResult result, string feedback, int hitIndex, int totalHits)
        {
            string bonusText = success ? "<color=green>+20% DAÑO EXTRA</color>" : "<color=gray>DAÑO BASE</color>";
            Debug.Log($"<color=lime>[QTE Evaluación]</color> Golpe {hitIndex}/{totalHits}: <b>{feedback}</b> ({bonusText})");
        }

        private void HandleManaInsufficient(CombatParticipant participant, SkillData skill, int required, int current)
        {
            Debug.LogWarning($"<color=red>[ALERTA]</color> ¡Maná insuficiente para {skill.SkillName}! Requiere: {required} MP | Tienes: {current} MP.");
        }

        private void HandleShieldChanged(CombatParticipant participant, int currentShield)
        {
            Debug.Log($"<color=cyan>[Escudo]</color> {participant.Name} tiene ahora <b>{currentShield}</b> de escudo de absorción.");
        }

        private void HandleQTEStarted(TimingWindowConfig config)
        {
            Debug.Log($"<color=yellow>[QTE]</color> Duración: {config.TotalDuration:F2}s.");
        }

        private void HandleQTEResolved(TimingResult result)
        {
            Debug.Log($"<color=yellow>[QTE Global]</color> Resultado general: <b>{result}</b>");
        }

        private void HandleDefenseWindowStarted(DefenseType defense, TimingWindowConfig config)
        {
            Debug.Log($"<color=orange>[¡Ataque Enemigo Inminente!]</color> Presiona [Z] para Parry o [X] para Dodge!");
        }

        private void HandleDefenseWindowResolved(DefenseType defense, TimingResult result)
        {
            Debug.Log($"<color=orange>[Defensa]</color> Acción: {defense} | Resultado: <b>{result}</b>");
        }

        private void HandleDamageResolved(CombatParticipant attacker, CombatParticipant defender, DamageResult result)
        {
            string critText = result.IsCritical ? " <color=red>[CRIT!]</color>" : "";
            string parryText = result.WasParried ? $" <color=green>[PARRY - Contraataque {result.CounterDamage} dmg!]</color>" : "";
            string dodgeText = result.WasDodged ? " <color=cyan>[ESQUIVADO!]</color>" : "";

            Debug.Log($"<color=red>[Resolución de Daño]</color> {attacker.Name} inflige {result.FinalDamage} daño a {defender.Name} (Base: {result.RawDamage}){critText}{parryText}{dodgeText}. Escudo: {defender.CurrentShield} | HP: {defender.CurrentHealth}/{defender.MaxHealth}");
        }

        private void HandleBattleFinished(bool playerWon)
        {
            string outcome = playerWon ? "<color=green>¡VICTORIA!</color>" : "<color=red>¡DERROTA!</color>";
            Debug.Log($"<color=white>================= FIN DEL COMBATE: {outcome} =================</color>");
        }
    }
}
