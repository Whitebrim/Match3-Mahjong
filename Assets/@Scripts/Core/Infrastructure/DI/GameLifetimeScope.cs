using Core.Infrastructure.StateMachine;
using Core.Infrastructure.StateMachine.States;
using Core.Infrastructure.StateMachine.States.Levels;
using Core.Services;
using Core.Services.AssetManagement;
using Core.Services.Audio;
using MessagePipe;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Core.Infrastructure.DI
{
    public class GameLifetimeScope : LifetimeScope
    {
        [SerializeField] private AudioSystem audioSystem;
        [SerializeField] private MainThreadDispatcher mainThreadDispatcher;
        [SerializeField] private ConditionalAssetManager conditionalAssetManager;

        protected override void Configure(IContainerBuilder builder)
        {
            if (GameBootstrapper.IsInitialized) return;
            builder.RegisterEntryPoint<GameBootstrapper>();//<-- Application entry point

            builder.Register<IObjectResolver, Container>(Lifetime.Scoped);
            
            builder.RegisterComponent(audioSystem);
            builder.RegisterComponent(mainThreadDispatcher);
            
            builder.RegisterInstance(conditionalAssetManager);

            builder.Register<GameStateMachine>(Lifetime.Singleton);
            builder.Register<BootstrapState>(Lifetime.Singleton);
            builder.Register<MainMenuState>(Lifetime.Singleton);
            builder.Register<SelectLevelState>(Lifetime.Singleton);
            
            builder.Register<Level1State>(Lifetime.Singleton);
            
            RegisterMessagePipe(builder);
        }

        private void RegisterMessagePipe(IContainerBuilder builder)
        {
            var options = builder.RegisterMessagePipe();
            
            builder.RegisterBuildCallback(c => GlobalMessagePipe.SetProvider(c.AsServiceProvider()));
        }
    }
}
