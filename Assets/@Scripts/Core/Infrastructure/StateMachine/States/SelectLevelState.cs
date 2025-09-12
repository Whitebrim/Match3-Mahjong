using Core.Infrastructure.StateMachine.States.Levels;
using Core.Services.SceneLoader;
using Core.Signals;
using Cysharp.Threading.Tasks;
using Levels;
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
            _changeUIVisibilitySignal.Publish(UIType.SelectLevel, new ChangeUIVisibilitySignal{Visible = true});
        }

        public void Exit()
        {
            _changeUIVisibilitySignal.Publish(UIType.SelectLevel, new ChangeUIVisibilitySignal{Visible = false});
        }

        [Button(ButtonSizes.Medium)]
        public async UniTask EnterLevel(Level level)
        {
            await SceneLoader.LoadSceneAsync(SceneNameConstants.Game);
            
            switch (level)
            {
                case Level.L1:
                    _stateMachine.Enter<Level1State>();
                    break;
            }
        }
    }
}