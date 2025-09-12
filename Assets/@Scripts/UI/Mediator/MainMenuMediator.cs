using Core.Infrastructure.StateMachine.States;
using Core.Signals;
using Levels;
using MessagePipe;
using Sirenix.OdinInspector;
using UnityEngine;
using VContainer;

namespace UI.Mediator
{
    public class MainMenuMediator : Mediator
    {
        [Inject] private readonly ISubscriber<UIType, ChangeUIVisibilitySignal> _changeUIVisibilitySignal;

        [SerializeField] private GameObject mainMenuUI;
        [SerializeField] private GameObject selectLevelUI;
        
        private void Awake()
        {
            var bag = DisposableBag.CreateBuilder();
            _changeUIVisibilitySignal.Subscribe(UIType.MainMenu, signal => mainMenuUI.SetActive(signal.Visible)).AddTo(bag);
            _changeUIVisibilitySignal.Subscribe(UIType.SelectLevel, signal => selectLevelUI.SetActive(signal.Visible)).AddTo(bag);
            Disposable = bag.Build();
        }
        
        [Button(ButtonSizes.Medium)]
        public void OpenSelectLevel()
        {
            StateMachine.Enter<SelectLevelState>();
        }

        [Button(ButtonSizes.Medium)]
        public void OpenMainMenu()
        {
            StateMachine.Enter<MainMenuState>();
        }
        
        [Button(ButtonSizes.Medium)]
        public void EnterLevel(int level)
        {
            if (StateMachine.CurrentState is SelectLevelState state)
            {
                _ = state.EnterLevel((Level)level);
            }
            else
            {
                StateMachine.Enter<SelectLevelState>();
                EnterLevel(level);
            }
        }
    }
}