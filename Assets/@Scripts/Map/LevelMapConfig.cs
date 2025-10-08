using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Map
{
    public class LevelMapConfig : SerializedScriptableObject
    {
        [Title("Decoration Settings")]
        public float decorationDensity = 0.3f;
        [Range(-500f, -100f)]
        public float leftDecorationOffset = -300f;
        [Range(100f, 500f)]
        public float rightDecorationOffset = 300f;
        public float decorationSpreadRange = 100f;
        
        [Title("Prefabs")]
        public AssetReferenceT<GameObject> levelButtonPrefab;
        public AssetReferenceT<GameObject> roadSegmentPrefab;
        
        [Title("Biomes")]
        public List<BiomeConfig> biomes = new();
        
        [Title("Biome Sequence")]
        [InfoBox("The order in which biomes appear on the map")]
        public List<int> biomeSequence = new() { 0, 1, 2, 1, 3 };
    }
    
    [System.Serializable]
    public class BiomeConfig
    {
        [Title("General")]
        public string biomeName = "Forest";
        public Color topGradientColor = new Color(0.4f, 0.8f, 0.3f);
        public Color bottomGradientColor = new Color(0.2f, 0.5f, 0.2f);
        
        [Title("Decorations")]
        [TableList]
        public List<DecorationItem> decorations = new();
        
        [Title("Road Appearance")]
        public Color roadTint = Color.white;
    }
    
    [System.Serializable]
    public class DecorationItem
    {
        [PreviewField(50)]
        public AssetReferenceSprite sprite;
        
        [Range(0f, 1f)]
        public float spawnChance = 0.5f;
        
        [MinMaxSlider(0.5f, 2f)]
        public Vector2 scaleRange = new Vector2(0.8f, 1.2f);
        
        public bool canFlipHorizontally = true;
        
        [Range(-180f, 180f)]
        public float maxRotation = 15f;
        
        public int sortingOrder = 1;
    }
}