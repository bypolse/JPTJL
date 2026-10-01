namespace JPTJL.Combat
{
    /// <summary>
    /// Explicit phases defining the flow of turn-based combat.
    /// </summary>
    public enum CombatPhase
    {
        BattleStart,
        PlayerTurn,
        ActionSelection,
        QTEExecution,
        EnemyTurn,
        DefenseWindow,
        DamageResolution,
        BattleEnd
    }
}
