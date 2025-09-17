using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Game.Tiles
{
    [CreateAssetMenu(menuName = "Scriptable Objects/TileDictionaryConfig", fileName = "new TileDictionaryConfig")]
    public class TileDictionaryConfig : SerializedScriptableObject
    {
        public readonly Dictionary<TileType, AssetReferenceGameObject> TilePrefabs = new();
    }
}