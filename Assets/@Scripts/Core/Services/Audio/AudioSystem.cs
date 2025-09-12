using UnityEngine;
using Utils.Extensions;

namespace Core.Services.Audio
{
    public class AudioSystem : MonoBehaviour
    {
        [SerializeField] private AudioSource music;
        [SerializeField] private AudioSource effects;

        public void ChangeVolume(AudioType audioType, float value)
        {
            switch (audioType)
            {
                case AudioType.Music:
                    music.volume = value;
                    break;
                case AudioType.Sfx:
                    effects.volume = value;
                    break;
            }
        }

        public async void PlayClip(AudioBase sound, AudioType audioType)
        {
            if (sound?.clip is null) return;
            var clip = await sound.clip.LoadAndCacheAsync(sound.releaseKey);
            PlayClip(clip, audioType, sound.volume);
        }

        private void PlayClip(AudioClip clip, AudioType audioType, float volumeScale = 1)
        {
            if (clip is null) return;
            switch (audioType)
            {
                case AudioType.Music:
                    music.PlayOneShot(clip, volumeScale);
                    break;
                case AudioType.Sfx:
                    effects.PlayOneShot(clip, volumeScale);
                    break;
            }
        }
    }
}