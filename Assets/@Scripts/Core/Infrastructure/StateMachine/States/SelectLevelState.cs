using Core.Services.AssetManagement;
using Core.Services.SceneLoader;
using Core.Signals;
using Cysharp.Threading.Tasks;
using MessagePipe;
using Sirenix.OdinInspector;
using UI;
using VContainer;

namespace Core.Infrastructure.StateMachine.States
{
    public class SelectLevelState : IState
    {
        [Inject] private readonly GameStateMachine _stateMachine;
        [Inject] private readonly IPublisher<UIType, ChangeUIVisibilitySignal> _changeUIVisibilitySignal;

        public void Enter()
        {
            //_changeUIVisibilitySignal.Publish(UIType.SelectLevel, new ChangeUIVisibilitySignal{Visible = true});
        }

        public void Exit()
        {
            //_changeUIVisibilitySignal.Publish(UIType.SelectLevel, new ChangeUIVisibilitySignal{Visible = false});
        }

        [Button(ButtonSizes.Medium)]
        public async UniTask EnterLevel(ulong level)
        {
            await SceneLoader.LoadSceneAsync(SceneNameConstants.Game);
            AddressablesCache.ReleaseAssets(ReleaseKey.MainMenu);
            _stateMachine.Enter<GameState, ulong>(level);
        }
    }
}