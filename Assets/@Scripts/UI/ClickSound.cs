using Core.Services.Audio;
using Sirenix.OdinInspector;
using Solo.MOST_IN_ONE;
using UnityEngine;
using UnityEngine.UI;
using Utils.Extensions;
using VContainer;
using AudioType = Core.Services.Audio.AudioType;

namespace UI
{
    [RequireComponent(typeof(Button))]
    public class ClickSound : SerializedMonoBehaviour
    {
        [Inject] private readonly AudioSystem _audioSystem;
        
        [SerializeField] private AudioBase sound;
        [SerializeField] private bool preloadSoundClip = true;
        [SerializeField] private bool vibrate = true;
        [ShowIf("vibrate"), SerializeField]
        private Most_HapticFeedback.HapticTypes vibration = Most_HapticFeedback.HapticTypes.Selection;
        
        private Button _button;

        private void Start()
        {
            if (sound == null) return;

            _button = GetComponent<Button>();
            InjectSound();
            if (preloadSoundClip) PreloadSoundClip();
        }

        private void InjectSound()
        {
            _button.onClick.AddListener(PlaySound);
        }

        private async void PreloadSoundClip()
        {
            await sound.clip.LoadAndCacheAsync(sound.releaseKey);
        }
        
        private void PlaySound()
        {
            _audioSystem.PlayClip(sound, AudioType.Sfx);
            if (vibrate)
                Most_HapticFeedback.Generate(vibration);
        }
    }
}
