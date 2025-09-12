using VContainer;

namespace Core.Infrastructure.StateMachine
{
    public class GameStateMachine
    {
        [Inject] private readonly IObjectResolver _resolver;
        
        public IBaseState CurrentState { get; private set; }

        public void Enter<TState>() where TState : class, IState
        {
            var state = ChangeState<TState>();
            state.Enter();
        }

        public void Enter<TState, TPayload>(TPayload payload) where TState : class, IPayloadedState<TPayload>
        {
            var state = ChangeState<TState>();
            state.Enter(payload);
        }

        private TState GetState<TState>() where TState : class, IBaseState =>
            _resolver.Resolve<TState>();

        private TState ChangeState<TState>() where TState : class, IBaseState
        {
            CurrentState?.Exit();

            var state = GetState<TState>();
            CurrentState = state;

            return state;
        }
    }
}