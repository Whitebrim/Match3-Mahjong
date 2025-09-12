using System;
using Sirenix.OdinInspector;
using UnityEngine.AddressableAssets;
using UnityEngine;
using Utils.Extensions;

namespace Core.Services.AssetManagement
{
    public class ConditionalAssetManager : SerializedScriptableObject
    {
        //Example
        //[HideLabel, BoxGroup(GroupID = "Nakama Connection", CenterLabel = true, LabelText = "Nakama Connection", ShowLabel = true)] public ConditionalAsset<NakamaConnection> NakamaConnection;
    }

    [Serializable]
    public class ConditionalAsset<TObj> where TObj : UnityEngine.Object
    {
        public TObj Load(string releaseKey) => Debug.isDebugBuild ? debug.LoadAndCache(releaseKey) : release.LoadAndCache(releaseKey);

        [SerializeField, HorizontalGroup(LabelWidth = 60, MarginLeft = 0.01f)] private AssetReferenceT<TObj> release;
        [SerializeField, HorizontalGroup(MarginLeft = 0.05f, MarginRight = 0.01f)] private AssetReferenceT<TObj> debug;
    }
}