using Core.Signals;
using MessagePipe;
using UI;
using VContainer;

namespace Core.Infrastructure.StateMachine.States
{
    public class MainMenuState : IState
    {
        [Inject] private readonly GameStateMachine _stateMachine;
        [Inject] private readonly IPublisher<UIType, ChangeUIVisibilitySignal> _changeUIVisibilitySignal;

        public void Enter()
        {
            _changeUIVisibilitySignal.Publish(UIType.MainMenu, new ChangeUIVisibilitySignal{Visible = true});
        }

        public void Exit()
        {
            _changeUIVisibilitySignal.Publish(UIType.MainMenu, new ChangeUIVisibilitySignal{Visible = false});
        }
    }
}