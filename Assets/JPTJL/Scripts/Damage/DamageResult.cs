using JPTJL.Combat;

namespace JPTJL.Damage
{
    /// <summary>
    /// Detailed report of a resolved damage interaction, including all offensive and defensive modifiers.
    /// </summary>
    public struct DamageResult
    {
        public int RawDamage;
        public int FinalDamage;
        public bool IsCritical;
        public TimingResult AttackTiming;
        public DefenseType DefenseAction;
        public TimingResult DefenseTiming;
        public bool WasParried;
        public bool WasDodged;
        public int CounterDamage;

        public DamageResult(int rawDamage, int finalDamage, bool isCritical, TimingResult attackTiming,
            DefenseType defenseAction, TimingResult defenseTiming, bool wasParried, bool wasDodged, int counterDamage)
        {
            RawDamage = rawDamage;
            FinalDamage = finalDamage;
            IsCritical = isCritical;
            AttackTiming = attackTiming;
            DefenseAction = defenseAction;
            DefenseTiming = defenseTiming;
            WasParried = wasParried;
            WasDodged = wasDodged;
            CounterDamage = counterDamage;
        }
    }
}
