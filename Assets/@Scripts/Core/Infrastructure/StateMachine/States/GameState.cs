using Core.Services.AssetManagement;
using Core.Services.SceneLoader;
using VContainer;

namespace Core.Infrastructure.StateMachine.States
{
    public class GameState : IPayloadedState<int>
    {
        [Inject] private readonly GameStateMachine _stateMachine;
        [Inject] private readonly IObjectResolver _resolver;
        
        public int Level { get; private set; }
        
        public void Enter(int payload)
        {
            Level = payload;
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