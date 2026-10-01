using System;

namespace JPTJL.FSM
{
    /// <summary>
    /// Reusable and clean Finite State Machine implementation for combat and gameplay flows.
    /// </summary>
    public class StateMachine
    {
        public IState CurrentState { get; private set; }
        public IState PreviousState { get; private set; }

        public event Action<IState, IState> OnStateChanged;

        public void Initialize(IState startingState)
        {
            if (startingState == null)
            {
                throw new ArgumentNullException(nameof(startingState));
            }

            CurrentState = startingState;
            CurrentState.Enter();
        }

        public void ChangeState(IState newState)
        {
            if (newState == null || CurrentState == newState)
            {
                return;
            }

            PreviousState = CurrentState;
            CurrentState?.Exit();

            CurrentState = newState;
            CurrentState.Enter();

            OnStateChanged?.Invoke(PreviousState, CurrentState);
        }

        public void Update(float deltaTime)
        {
            CurrentState?.Update(deltaTime);
        }
    }
}
