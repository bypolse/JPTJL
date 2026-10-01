using System;
using UnityEngine;
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
        public static event Action<CombatParticipant, SkillData, int, int> OnManaInsufficient; // participant, skill, requiredMp, currentMp

        // Offensive QTE Events (Legacy / Precision Timing)
        public static event Action<TimingWindowConfig> OnQTEStarted;
        public static event Action<float, float> OnQTETicked; // normalizedProgress, elapsedTime
        public static event Action<TimingResult> OnQTEResolved;

        // Key QTE Events (Random Key Prompt + Countdown Timer)
        public static event Action<KeyCode, string, float, int, int> OnKeyQTEPrompt; // key, keyName, duration, currentHit, totalHits
        public static event Action<float, float> OnKeyQTETicked; // remainingTime, normalizedProgress
        public static event Action<KeyCode, bool, TimingResult, string, int, int> OnKeyQTEEvaluated; // key, success, result, feedbackText, currentHit, totalHits
        public static event Action<string, Color> OnFloatingFeedback; // text, color

        // Defensive Timing Events (Parry / Dodge)
        public static event Action<DefenseType, TimingWindowConfig> OnDefenseWindowStarted;
        public static event Action<DefenseType, float, float> OnDefenseWindowTicked;
        public static event Action<DefenseType, TimingResult> OnDefenseWindowResolved;

        // Damage & Health / Shield / Mana Resolution
        public static event Action<CombatParticipant, CombatParticipant, DamageResult> OnDamageResolved;
        public static event Action<CombatParticipant, int, int> OnHealthChanged; // participant, currentHp, maxHp
        public static event Action<CombatParticipant, int, int> OnManaChanged; // participant, currentMp, maxMp
        public static event Action<CombatParticipant, int> OnShieldChanged; // participant, currentShield
        public static event Action<CombatParticipant> OnParticipantDefeated;

        // Battle conclusion
        public static event Action<bool> OnBattleFinished; // true = Player Victory, false = Defeat

        // Internal trigger dispatchers (invoked only by Combat Core)
        internal static void TriggerPhaseChanged(CombatPhase phase) => OnPhaseChanged?.Invoke(phase);
        internal static void TriggerTurnStarted(CombatParticipant participant) => OnTurnStarted?.Invoke(participant);
        internal static void TriggerSkillSelected(CombatParticipant participant, SkillData skill) => OnSkillSelected?.Invoke(participant, skill);
        internal static void TriggerManaInsufficient(CombatParticipant participant, SkillData skill, int required, int current) => OnManaInsufficient?.Invoke(participant, skill, required, current);

        internal static void TriggerQTEStarted(TimingWindowConfig config) => OnQTEStarted?.Invoke(config);
        internal static void TriggerQTETicked(float progress, float elapsed) => OnQTETicked?.Invoke(progress, elapsed);
        internal static void TriggerQTEResolved(TimingResult result) => OnQTEResolved?.Invoke(result);

        internal static void TriggerKeyQTEPrompt(KeyCode key, string keyName, float duration, int hit, int total) => OnKeyQTEPrompt?.Invoke(key, keyName, duration, hit, total);
        internal static void TriggerKeyQTETicked(float remaining, float progress) => OnKeyQTETicked?.Invoke(remaining, progress);
        internal static void TriggerKeyQTEEvaluated(KeyCode key, bool success, TimingResult result, string feedback, int hit, int total) => OnKeyQTEEvaluated?.Invoke(key, success, result, feedback, hit, total);
        internal static void TriggerFloatingFeedback(string text, Color color) => OnFloatingFeedback?.Invoke(text, color);

        internal static void TriggerDefenseWindowStarted(DefenseType type, TimingWindowConfig config) => OnDefenseWindowStarted?.Invoke(type, config);
        internal static void TriggerDefenseWindowTicked(DefenseType type, float progress, float elapsed) => OnDefenseWindowTicked?.Invoke(type, progress, elapsed);
        internal static void TriggerDefenseWindowResolved(DefenseType type, TimingResult result) => OnDefenseWindowResolved?.Invoke(type, result);
        internal static void TriggerDamageResolved(CombatParticipant attacker, CombatParticipant defender, DamageResult result) => OnDamageResolved?.Invoke(attacker, defender, result);
        internal static void TriggerHealthChanged(CombatParticipant participant, int currentHp, int maxHp) => OnHealthChanged?.Invoke(participant, currentHp, maxHp);
        internal static void TriggerManaChanged(CombatParticipant participant, int currentMp, int maxMp) => OnManaChanged?.Invoke(participant, currentMp, maxMp);
        internal static void TriggerShieldChanged(CombatParticipant participant, int currentShield) => OnShieldChanged?.Invoke(participant, currentShield);
        internal static void TriggerParticipantDefeated(CombatParticipant participant) => OnParticipantDefeated?.Invoke(participant);
        internal static void TriggerBattleFinished(bool playerWon) => OnBattleFinished?.Invoke(playerWon);
    }
}
