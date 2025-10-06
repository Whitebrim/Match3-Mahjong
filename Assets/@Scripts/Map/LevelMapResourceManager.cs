using System.Collections.Generic;
using Core.Services.AssetManagement;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using Utils.Extensions;

namespace Map
{
    /// <summary>
    /// Менеджер для предзагрузки ресурсов карты уровней.
    /// Использует AddressablesCache для кеширования через LoadAndCache
    /// </summary>
    public class LevelMapResourceManager : MonoBehaviour
    {
        private static LevelMapResourceManager _instance;
        public static LevelMapResourceManager Instance => _instance;
        
        [Header("Preload Settings")]
        [SerializeField] private bool preloadBiomesOnStart = true;
        [SerializeField] private int biomesToPreload = 2; // Сколько следующих биомов предзагружать
        
        private LevelMapConfig _config;
        private int _currentBiomeIndex = 0;
        private readonly HashSet<int> _preloadedBiomes = new();
        
        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
        
        public void Initialize(LevelMapConfig config)
        {
            _config = config;
            
            if (preloadBiomesOnStart)
            {
                PreloadNextBiomes();
            }
        }
        
        /// <summary>
        /// Уведомляет менеджер о текущем биоме для предзагрузки следующих
        /// </summary>
        public void SetCurrentBiome(int biomeIndex)
        {
            _currentBiomeIndex = biomeIndex;
            PreloadNextBiomes();
        }
        
        /// <summary>
        /// Предзагружает ресурсы следующих биомов
        /// </summary>
        private void PreloadNextBiomes()
        {
            if (_config == null) return;
            
            PreloadNextBiomesAsync().Forget();
        }
        
        private async UniTaskVoid PreloadNextBiomesAsync()
        {
            for (int i = 0; i < biomesToPreload; i++)
            {
                int biomeToLoad = (_currentBiomeIndex + i) % _config.biomeSequence.Count;
                
                // Проверяем, не загружен ли уже этот биом
                if (_preloadedBiomes.Contains(biomeToLoad))
                {
                    continue;
                }
                
                int biomeId = _config.biomeSequence[biomeToLoad];
                if (biomeId >= 0 && biomeId < _config.biomes.Count)
                {
                    await PreloadBiomeResourcesAsync(_config.biomes[biomeId]);
                    _preloadedBiomes.Add(biomeToLoad);
                }
            }
            
            // Очищаем старые биомы из кеша предзагрузки
            CleanupOldBiomes();
        }
        
        /// <summary>
        /// Предзагружает все ресурсы биома
        /// </summary>
        public async UniTask PreloadBiomeResourcesAsync(BiomeConfig biome)
        {
            if (biome == null) return;
            
            var tasks = new List<UniTask>();
            
            // Предзагружаем спрайты декораций
            foreach (var decoration in biome.decorations)
            {
                if (decoration.sprite != null)
                {
                    tasks.Add(decoration.sprite.LoadAndCacheAsync<Sprite>(ReleaseKey.MainMenu));
                }
            }
            
            // Предзагружаем спрайт дороги
            if (biome.roadSprite != null)
            {
                tasks.Add(biome.roadSprite.LoadAndCacheAsync<Sprite>(ReleaseKey.MainMenu));
            }
            
            // Ждем загрузки всех ресурсов
            await UniTask.WhenAll(tasks);
            
            Debug.Log($"Preloaded biome: {biome.biomeName}");
        }
        
        /// <summary>
        /// Очищает из памяти биомы, которые далеко от текущего
        /// </summary>
        private void CleanupOldBiomes()
        {
            var biomesToKeep = new HashSet<int>();
            
            // Определяем какие биомы нужно оставить
            for (int i = -1; i <= biomesToPreload; i++)
            {
                int biomeIndex = (_currentBiomeIndex + i + _config.biomeSequence.Count) % _config.biomeSequence.Count;
                biomesToKeep.Add(biomeIndex);
            }
            
            // Удаляем из списка предзагруженных те, которые больше не нужны
            _preloadedBiomes.RemoveWhere(b => !biomesToKeep.Contains(b));
            
            // AddressablesCache автоматически управляет памятью через ReleaseKey
        }
        
        /// <summary>
        /// Загружает спрайт декорации (использует кеш AddressablesCache)
        /// </summary>
        public async UniTask<Sprite> LoadDecorationSprite(AssetReferenceT<Sprite> spriteRef)
        {
            if (spriteRef == null) return null;
            
            return await spriteRef.LoadAndCacheAsync<Sprite>(ReleaseKey.MainMenu);
        }
        
        /// <summary>
        /// Загружает префаб (использует кеш AddressablesCache)
        /// </summary>
        public async UniTask<GameObject> LoadPrefab(AssetReferenceT<GameObject> prefabRef)
        {
            if (prefabRef == null) return null;
            
            return await prefabRef.LoadAndCacheAsync<GameObject>(ReleaseKey.MainMenu);
        }
        
        /// <summary>
        /// Загружает спрайт декорации синхронно
        /// </summary>
        public Sprite LoadDecorationSpriteSync(AssetReferenceT<Sprite> spriteRef)
        {
            if (spriteRef == null) return null;
            
            return spriteRef.LoadAndCache<Sprite>(ReleaseKey.MainMenu);
        }
        
        /// <summary>
        /// Загружает префаб синхронно
        /// </summary>
        public GameObject LoadPrefabSync(AssetReferenceT<GameObject> prefabRef)
        {
            if (prefabRef == null) return null;
            
            return prefabRef.LoadAndCache<GameObject>(ReleaseKey.MainMenu);
        }
    }
}