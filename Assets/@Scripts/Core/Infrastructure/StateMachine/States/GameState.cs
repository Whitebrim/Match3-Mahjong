using Core.Services.AssetManagement;
using Core.Services.SceneLoader;
using VContainer;

namespace Core.Infrastructure.StateMachine.States
{
    public class GameState : IPayloadedState<ulong>
    {
        [Inject] private readonly GameStateMachine _stateMachine;
        [Inject] private readonly IObjectResolver _resolver;
        
        public void Enter(ulong payload)
        {
            
        }

        public void Exit()
        {
            AddressablesCache.ReleaseAssets(ReleaseKey.Game);
        }

        public async void GameOver()
        {
            await SceneLoader.LoadSceneAsync(SceneNameConstants.MainMenu);
            _stateMachine.Enter<MainMenuState>();
        }
    }
}