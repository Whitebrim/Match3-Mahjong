using System;
using Core.Services.AssetManagement;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Core.Services.Audio
{
    [Serializable]
    public class AudioBase
    {
        [Required] public AssetReferenceT<AudioClip> clip;
        [Range(0, 1)] public float volume = 1;
        public string releaseKey = ReleaseKey.Permanent;
    }
}