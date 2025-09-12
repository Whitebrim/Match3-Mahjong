using Core.Signals;
using MessagePipe;
using UnityEngine;
using VContainer;

namespace Utils
{
    public class ApplicationFocus : MonoBehaviour
    {
        [Inject] private readonly IPublisher<OnApplicationFocusSignal> _publisher;

        private void OnApplicationFocus(bool hasFocus)
        {
            _publisher.Publish(new OnApplicationFocusSignal{HasFocus = hasFocus});
        }
    }
}