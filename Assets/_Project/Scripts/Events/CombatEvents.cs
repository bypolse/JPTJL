using System;
using Project.Combat.Data;
using Project.Combat.UI;

namespace Project.Combat.Events
{
    /// <summary>
    /// Static event bus decoupling the combat logic, state, and UI.
    /// Provides atomic events and safe trigger helpers.
    /// </summary>
    public static class CombatEvents
    {
        public static event Action<SkillData> OnQTETriggered;
        public static event Action<TimingResult> OnQTECompleted;
        public static event Action<string, int, int> OnUnitHealthChanged; // (unitId, currentHp, maxHp)

        public static void TriggerQTE(SkillData skill) => OnQTETriggered?.Invoke(skill);
        public static void CompleteQTE(TimingResult result) => OnQTECompleted?.Invoke(result);
        public static void UpdateHealth(string unitId, int currentHp, int maxHp) => OnUnitHealthChanged?.Invoke(unitId, currentHp, maxHp);
    }
}
