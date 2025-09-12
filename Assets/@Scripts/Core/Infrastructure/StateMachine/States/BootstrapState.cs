using Core.Services.SceneLoader;
using VContainer;

namespace Core.Infrastructure.StateMachine.States
{
    public class BootstrapState : IState
    {
        [Inject] private readonly GameStateMachine _stateMachine;

        public void Enter()
        {
            LoadMainMenu();
        }

        public void Exit(){}

        private async void LoadMainMenu()
        {
            await SceneLoader.LoadSceneAsync(SceneNameConstants.MainMenu);
            _stateMachine.Enter<MainMenuState>();
        }
    }
}