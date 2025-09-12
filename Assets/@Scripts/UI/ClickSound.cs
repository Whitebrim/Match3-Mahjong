using Core.Services.Audio;
using UnityEngine;
using UnityEngine.UI;
using Utils.Extensions;
using VContainer;
using AudioType = Core.Services.Audio.AudioType;

namespace UI
{
    [RequireComponent(typeof(Button))]
    public class ClickSound : MonoBehaviour
    {
        [Inject] private readonly AudioSystem _audioSystem;
        
        [SerializeField] private AudioBase sound;
        [SerializeField] private bool preloadSoundClip = true;
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
        }
    }
}
