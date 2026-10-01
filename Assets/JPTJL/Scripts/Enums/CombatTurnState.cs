namespace JPTJL.Combat
{
    /// <summary>
    /// Explicit high-level turn states matching game design rules:
    /// TurnoJugador, EjecutandoQTE, TurnoEnemigo, FinCombate.
    /// </summary>
    public enum CombatTurnState
    {
        TurnoJugador,
        EjecutandoQTE,
        TurnoEnemigo,
        FinCombate
    }
}
