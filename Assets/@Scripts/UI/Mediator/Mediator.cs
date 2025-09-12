using System;
using Core.Infrastructure.StateMachine;
using Sirenix.OdinInspector;
using VContainer;

namespace UI.Mediator
{
    public abstract class Mediator : SerializedMonoBehaviour
    {
        [Inject] protected readonly GameStateMachine StateMachine;

        protected IDisposable Disposable;

        public void OnDestroy()
        {
            Disposable?.Dispose();
        }
    }
}
