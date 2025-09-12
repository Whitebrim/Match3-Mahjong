using Core.Services.AssetManagement;
using Levels.Configs;
using Sirenix.OdinInspector;
using UI;
using UnityEngine.AddressableAssets;
using Utils.Extensions;
using VContainer;
using VContainer.Unity;

namespace Core.Infrastructure.StateMachine.States.Levels
{
    public class Level1State : IState
    {
        [Inject] private readonly GameStateMachine _stateMachine;
        [Inject] private readonly IObjectResolver _resolver;
        
        private static readonly AssetReferenceT<Level1Config> ConfigReference = new("Level 1 Config");
        [ShowInInspector]
        private Level1Config _config;
        
        [Button]
        public async void Enter()
        {
            CanvasRoot.Clear();
            _config = await ConfigReference.LoadAndCacheAsync(ReleaseKey.Level1);
            _resolver.Instantiate(_config.prefab, CanvasRoot.Root);
        }

        public void Exit()
        {
            AddressablesCache.ReleaseAssets(ReleaseKey.Level1);
        }
    }
}