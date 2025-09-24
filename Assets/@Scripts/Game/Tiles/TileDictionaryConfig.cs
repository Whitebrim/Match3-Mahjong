using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine.AddressableAssets;

namespace Game.Tiles
{
    public class TileDictionaryConfig : SerializedScriptableObject
    {
        public readonly TileView TilePrefab;
        public readonly Dictionary<TileType, AssetReferenceSprite> TileSprites = new();
    }
}