namespace JPTJL.FSM
{
    /// <summary>
    /// Contract for finite state machine states.
    /// </summary>
    public interface IState
    {
        void Enter();
        void Update(float deltaTime);
        void Exit();
    }
}
