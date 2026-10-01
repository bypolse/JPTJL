using System;
using JPTJL.Damage;
using JPTJL.Data;
using JPTJL.Timing;

namespace JPTJL.Combat
{
    /// <summary>
    /// Pure C# decoupled event bus for combat.
    /// External systems (Reactive UI, Audio, Particle VFX, Animations) subscribe to these events.
    /// The combat core NEVER holds references to visual or UI elements.
    /// </summary>
    public static class CombatEvents
    {
        // Phase lifecycle
        public static event Action<CombatPhase> OnPhaseChanged;

        // Turn management
        public static event Action<CombatParticipant> OnTurnStarted;

        // Action & Skills
        public static event Action<CombatParticipant, SkillData> OnSkillSelected;

        // Offensive QTE Events
        public static event Action<TimingWindowConfig> OnQTEStarted;
        public static event Action<float, float> OnQTETicked; // normalizedProgress, elapsedTime
        public static event Action<TimingResult> OnQTEResolved;

        // Defensive Timing Events (Parry / Dodge)
        public static event Action<DefenseType, TimingWindowConfig> OnDefenseWindowStarted;
        public static event Action<DefenseType, float, float> OnDefenseWindowTicked;
        public static event Action<DefenseType, TimingResult> OnDefenseWindowResolved;

        // Damage & Health Resolution
        public static event Action<CombatParticipant, CombatParticipant, DamageResult> OnDamageResolved;
        public static event Action<CombatParticipant, int, int> OnHealthChanged; // participant, currentHp, maxHp
        public static event Action<CombatParticipant> OnParticipantDefeated;

        // Battle conclusion
        public static event Action<bool> OnBattleFinished; // true = Player Victory, false = Defeat

        // Internal trigger dispatchers (invoked only by Combat Core)
        internal static void TriggerPhaseChanged(CombatPhase phase) => OnPhaseChanged?.Invoke(phase);
        internal static void TriggerTurnStarted(CombatParticipant participant) => OnTurnStarted?.Invoke(participant);
        internal static void TriggerSkillSelected(CombatParticipant participant, SkillData skill) => OnSkillSelected?.Invoke(participant, skill);
        internal static void TriggerQTEStarted(TimingWindowConfig config) => OnQTEStarted?.Invoke(config);
        internal static void TriggerQTETicked(float progress, float elapsed) => OnQTETicked?.Invoke(progress, elapsed);
        internal static void TriggerQTEResolved(TimingResult result) => OnQTEResolved?.Invoke(result);
        internal static void TriggerDefenseWindowStarted(DefenseType type, TimingWindowConfig config) => OnDefenseWindowStarted?.Invoke(type, config);
        internal static void TriggerDefenseWindowTicked(DefenseType type, float progress, float elapsed) => OnDefenseWindowTicked?.Invoke(type, progress, elapsed);
        internal static void TriggerDefenseWindowResolved(DefenseType type, TimingResult result) => OnDefenseWindowResolved?.Invoke(type, result);
        internal static void TriggerDamageResolved(CombatParticipant attacker, CombatParticipant defender, DamageResult result) => OnDamageResolved?.Invoke(attacker, defender, result);
        internal static void TriggerHealthChanged(CombatParticipant participant, int currentHp, int maxHp) => OnHealthChanged?.Invoke(participant, currentHp, maxHp);
        internal static void TriggerParticipantDefeated(CombatParticipant participant) => OnParticipantDefeated?.Invoke(participant);
        internal static void TriggerBattleFinished(bool playerWon) => OnBattleFinished?.Invoke(playerWon);
    }
}
