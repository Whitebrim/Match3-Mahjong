using System;
using System.Threading;
using Core.Infrastructure.StateMachine;
using Core.Infrastructure.StateMachine.States;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using VContainer;
using VContainer.Unity;

namespace Core.Infrastructure
{
    public class GameBootstrapper : IAsyncStartable
    {
        public static bool IsInitialized;
        
        [Inject] private readonly GameStateMachine _stateMachine;
        
        public async UniTask StartAsync(CancellationToken cancellation = default)
        {
            IsInitialized = true;
        
            await ApplicationInit();

            _stateMachine.Enter<BootstrapState>();
        }
        
        private async UniTask ApplicationInit()
        {
            Application.targetFrameRate = (int)Math.Ceiling(Screen.currentResolution.refreshRateRatio.value);
            await Addressables.InitializeAsync();
        }
    }
}