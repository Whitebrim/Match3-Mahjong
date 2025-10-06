using System.Collections.Generic;
using Core.Services.AssetManagement;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using Utils.Extensions;
using VContainer;
using VContainer.Unity;

namespace Map
{
    public class LevelMapController : MonoBehaviour
    {
        [Inject] private readonly IObjectResolver _resolver;
        
        [Header("References")]
        [SerializeField] private LevelMapConfig config;
        [SerializeField] private ScrollRect scrollRect;
        [SerializeField] private RectTransform contentContainer;
        [SerializeField] private Material backgroundMaterial;
        
        [Header("Pool Settings")]
        [SerializeField] private int segmentPoolSize = 10;
        [SerializeField] private float segmentHeight = 1000f;
        [SerializeField] private float loadThreshold = 500f;
        
        private readonly Queue<MapSegment> _segmentPool = new();
        private readonly List<MapSegment> _activeSegments = new();
        private readonly Dictionary<int, GameObject> _levelButtonCache = new();
        
        private float _lastGeneratedY;
        private int _currentLevelIndex;
        private int _currentBiomeIndex;
        private float _biomeTransitionProgress;
        private bool _isInitialized;
        private Camera _mainCamera;
        private Vector3[] _worldCorners = new Vector3[4];
        
        private GameObject _levelButtonPrefab;
        
        private class MapSegment
        {
            public GameObject gameObject;
            public RectTransform rectTransform;
            public float topY;
            public float bottomY;
            public int startLevel;
            public int endLevel;
            public List<GameObject> decorations = new();
            public List<GameObject> levelButtons = new();
        }
        
        private async void Start()
        {
            _mainCamera = Camera.main;
            await InitializeAsync();
        }
        
        private async UniTask InitializeAsync()
        {
            // Загружаем префаб кнопки уровня используя LoadAndCacheAsync
            _levelButtonPrefab = await config.levelButtonPrefab.LoadAndCacheAsync<GameObject>(ReleaseKey.MainMenu);
            
            // Инициализируем пул сегментов
            for (int i = 0; i < segmentPoolSize; i++)
            {
                var segment = CreateSegment();
                _segmentPool.Enqueue(segment);
            }
            
            // Создаем начальные сегменты
            GenerateInitialSegments();
            
            // Подписываемся на события скролла
            scrollRect.onValueChanged.AddListener(OnScroll);
            
            _isInitialized = true;
        }
        
        private MapSegment CreateSegment()
        {
            var segmentGO = new GameObject("MapSegment");
            segmentGO.transform.SetParent(contentContainer, false);
            
            var rectTransform = segmentGO.AddComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0, 1);
            rectTransform.anchorMax = new Vector2(1, 1);
            rectTransform.sizeDelta = new Vector2(0, segmentHeight);
            
            var segment = new MapSegment
            {
                gameObject = segmentGO,
                rectTransform = rectTransform
            };
            
            segmentGO.SetActive(false);
            return segment;
        }
        
        private void GenerateInitialSegments()
        {
            _lastGeneratedY = 0;
            _currentLevelIndex = 1;
            _currentBiomeIndex = 0;
            
            // Генерируем начальные сегменты
            for (int i = 0; i < 3; i++)
            {
                GenerateNextSegment();
            }
            
            // Обновляем размер контента
            UpdateContentHeight();
        }
        
        private void GenerateNextSegment()
        {
            if (_segmentPool.Count == 0)
            {
                RecycleDistantSegments();
                if (_segmentPool.Count == 0) return;
            }
            
            var segment = _segmentPool.Dequeue();
            segment.gameObject.SetActive(true);
            
            // Позиционируем сегмент
            segment.rectTransform.anchoredPosition = new Vector2(0, -_lastGeneratedY);
            segment.topY = _lastGeneratedY;
            segment.bottomY = _lastGeneratedY + segmentHeight;
            segment.startLevel = _currentLevelIndex;
            
            // Генерируем контент сегмента
            GenerateSegmentContent(segment);
            
            _activeSegments.Add(segment);
            _lastGeneratedY += segmentHeight;
            
            UpdateBiomeGradient();
        }
        
        private void GenerateSegmentContent(MapSegment segment)
        {
            // Очищаем старый контент
            ClearSegmentContent(segment);
            
            int levelsInSegment = Mathf.FloorToInt(segmentHeight / config.levelButtonSpacing);
            var currentBiome = GetCurrentBiome();
            
            for (int i = 0; i < levelsInSegment; i++)
            {
                float y = i * config.levelButtonSpacing;
                
                // Генерируем позицию на извилистой дорожке
                float pathX = GeneratePathX(segment.startLevel + i);
                
                // Создаем кнопку уровня
                CreateLevelButton(segment, segment.startLevel + i, new Vector2(pathX, -y));
                
                // Генерируем декорации
                if (UnityEngine.Random.value < config.decorationDensity)
                {
                    GenerateDecorations(segment, currentBiome, y, pathX);
                }
                
                _currentLevelIndex++;
                
                // Проверяем переход на новый биом
                if (_currentLevelIndex % config.levelsPerBiome == 0)
                {
                    TransitionToNextBiome();
                }
            }
            
            segment.endLevel = segment.startLevel + levelsInSegment - 1;
        }
        
        private float GeneratePathX(int levelIndex)
        {
            // Используем синусоиду для создания извилистого пути
            float t = levelIndex * config.pathCurveFrequency;
            float curve = config.pathCurve.Evaluate(Mathf.Sin(t));
            return curve * config.pathCurveAmplitude * 200f; // 200f - базовая амплитуда в пикселях
        }
        
        private void CreateLevelButton(MapSegment segment, int levelNumber, Vector2 position)
        {
            GameObject button;
            
            // Используем кеш или создаем новую кнопку
            if (!_levelButtonCache.TryGetValue(levelNumber, out button))
            {
                if (_levelButtonPrefab != null)
                {
                    button = _resolver.Instantiate(_levelButtonPrefab, segment.rectTransform);
                    _levelButtonCache[levelNumber] = button;
                }
                else
                {
                    Debug.LogError("Level button prefab not loaded!");
                    return;
                }
            }
            else
            {
                button.transform.SetParent(segment.rectTransform, false);
            }
            
            var rectTransform = button.GetComponent<RectTransform>();
            rectTransform.anchoredPosition = position;
            
            // Настраиваем компонент кнопки
            var levelButton = button.GetComponent<LevelButton>();
            if (levelButton != null)
            {
                bool isUnlocked = LevelProgressManager.Instance.IsLevelUnlocked(levelNumber);
                int stars = LevelProgressManager.Instance.GetLevelStars(levelNumber);
                levelButton.Initialize(levelNumber, isUnlocked, stars);
            }
            
            // Настраиваем номер уровня (fallback если нет компонента)
            var levelText = button.GetComponentInChildren<TMPro.TextMeshProUGUI>();
            if (levelText != null && levelButton == null)
            {
                levelText.text = levelNumber.ToString();
            }
            
            segment.levelButtons.Add(button);
        }
        
        private void GenerateDecorations(MapSegment segment, BiomeConfig biome, float y, float pathX)
        {
            if (biome?.decorations == null || biome.decorations.Count == 0) return;
            
            // Используем сид для консистентной генерации
            int seed = segment.startLevel * 1337 + Mathf.FloorToInt(y);
            UnityEngine.Random.InitState(seed);
            
            // Выбираем сторону для декорации
            bool isLeftSide = UnityEngine.Random.value > 0.5f;
            float baseX = isLeftSide ? 
                pathX + config.leftDecorationOffset : 
                pathX + config.rightDecorationOffset;
            
            // Добавляем случайное смещение
            float offsetX = UnityEngine.Random.Range(-config.decorationSpreadRange, config.decorationSpreadRange);
            float finalX = baseX + offsetX;
            
            // Выбираем случайную декорацию
            var decoration = SelectRandomDecoration(biome);
            if (decoration != null)
            {
                CreateDecorationObject(segment, decoration, new Vector2(finalX, -y));
            }
        }
        
        private DecorationItem SelectRandomDecoration(BiomeConfig biome)
        {
            float totalChance = 0;
            foreach (var dec in biome.decorations)
            {
                totalChance += dec.spawnChance;
            }
            
            float random = UnityEngine.Random.Range(0, totalChance);
            float current = 0;
            
            foreach (var dec in biome.decorations)
            {
                current += dec.spawnChance;
                if (random <= current)
                {
                    return dec;
                }
            }
            
            return null;
        }
        
        private async void CreateDecorationObject(MapSegment segment, DecorationItem decoration, Vector2 position)
        {
            // Загружаем спрайт используя LoadAndCacheAsync
            var sprite = await decoration.sprite.LoadAndCacheAsync(ReleaseKey.MainMenu);
            
            if (sprite == null)
            {
                Debug.LogWarning("Failed to load decoration sprite");
                return;
            }
            
            var decorGO = new GameObject("Decoration");
            decorGO.transform.SetParent(segment.rectTransform, false);
            
            var rectTransform = decorGO.AddComponent<RectTransform>();
            rectTransform.anchoredPosition = position;
            
            var image = decorGO.AddComponent<Image>();
            image.sprite = sprite;
            image.SetNativeSize();
            
            // Применяем случайные трансформации
            float scale = UnityEngine.Random.Range(decoration.scaleRange.x, decoration.scaleRange.y);
            rectTransform.localScale = Vector3.one * scale;
            
            if (decoration.canFlipHorizontally && UnityEngine.Random.value > 0.5f)
            {
                rectTransform.localScale = new Vector3(-scale, scale, 1);
            }
            
            if (decoration.maxRotation > 0)
            {
                float rotation = UnityEngine.Random.Range(-decoration.maxRotation, decoration.maxRotation);
                rectTransform.rotation = Quaternion.Euler(0, 0, rotation);
            }
            
            var canvas = decorGO.AddComponent<Canvas>();
            canvas.overrideSorting = true;
            canvas.sortingOrder = decoration.sortingOrder;
            
            segment.decorations.Add(decorGO);
        }
        
        private void OnScroll(Vector2 scrollPosition)
        {
            if (!_isInitialized) return;
            
            // Проверяем видимые границы
            GetViewportBounds(out float viewTop, out float viewBottom);
            
            // Генерируем новые сегменты если нужно
            if (_lastGeneratedY - viewBottom < loadThreshold)
            {
                GenerateNextSegment();
                UpdateContentHeight();
            }
            
            // Переработка далеких сегментов
            RecycleDistantSegments();
        }
        
        private void GetViewportBounds(out float viewTop, out float viewBottom)
        {
            scrollRect.viewport.GetWorldCorners(_worldCorners);
            
            Vector3 topWorld = _worldCorners[1];
            Vector3 bottomWorld = _worldCorners[0];
            
            Vector3 topLocal = contentContainer.InverseTransformPoint(topWorld);
            Vector3 bottomLocal = contentContainer.InverseTransformPoint(bottomWorld);
            
            viewTop = -topLocal.y;
            viewBottom = -bottomLocal.y;
        }
        
        private void RecycleDistantSegments()
        {
            GetViewportBounds(out float viewTop, out float viewBottom);
            
            for (int i = _activeSegments.Count - 1; i >= 0; i--)
            {
                var segment = _activeSegments[i];
                
                // Если сегмент полностью вне видимости
                if (segment.bottomY < viewTop - 1000 || segment.topY > viewBottom + 1000)
                {
                    RecycleSegment(segment);
                    _activeSegments.RemoveAt(i);
                }
            }
        }
        
        private void RecycleSegment(MapSegment segment)
        {
            ClearSegmentContent(segment);
            segment.gameObject.SetActive(false);
            _segmentPool.Enqueue(segment);
        }
        
        private void ClearSegmentContent(MapSegment segment)
        {
            // Возвращаем кнопки уровней в кеш
            foreach (var button in segment.levelButtons)
            {
                button.transform.SetParent(transform, false);
                button.SetActive(false);
            }
            segment.levelButtons.Clear();
            
            // Удаляем декорации
            foreach (var decoration in segment.decorations)
            {
                Destroy(decoration);
            }
            segment.decorations.Clear();
        }
        
        private BiomeConfig GetCurrentBiome()
        {
            if (config.biomes.Count == 0) return null;
            
            int biomeId = config.biomeSequence[_currentBiomeIndex % config.biomeSequence.Count];
            return config.biomes[Mathf.Clamp(biomeId, 0, config.biomes.Count - 1)];
        }
        
        private void TransitionToNextBiome()
        {
            _currentBiomeIndex++;
            StartBiomeTransition();
        }
        
        private void StartBiomeTransition()
        {
            TransitionBiomeGradientAsync().Forget();
        }
        
        private async UniTaskVoid TransitionBiomeGradientAsync()
        {
            float transitionDuration = 2f;
            float elapsed = 0;
            
            while (elapsed < transitionDuration)
            {
                elapsed += Time.deltaTime;
                _biomeTransitionProgress = elapsed / transitionDuration;
                UpdateBiomeGradient();
                await UniTask.Yield();
            }
            
            _biomeTransitionProgress = 0;
        }
        
        private void UpdateBiomeGradient()
        {
            if (backgroundMaterial == null || config.biomes.Count == 0) return;
            
            var currentBiome = GetCurrentBiome();
            if (currentBiome == null) return;
            
            // Получаем следующий биом для плавного перехода
            int nextBiomeIndex = (_currentBiomeIndex + 1) % config.biomeSequence.Count;
            int nextBiomeId = config.biomeSequence[nextBiomeIndex];
            var nextBiome = config.biomes[Mathf.Clamp(nextBiomeId, 0, config.biomes.Count - 1)];
            
            // Устанавливаем цвета градиента
            backgroundMaterial.SetColor("_TopColor1", currentBiome.topGradientColor);
            backgroundMaterial.SetColor("_BottomColor1", currentBiome.bottomGradientColor);
            backgroundMaterial.SetColor("_TopColor2", nextBiome.topGradientColor);
            backgroundMaterial.SetColor("_BottomColor2", nextBiome.bottomGradientColor);
            backgroundMaterial.SetFloat("_BiomeBlend", _biomeTransitionProgress);
        }
        
        private void UpdateContentHeight()
        {
            if (_activeSegments.Count == 0) return;
            
            float maxY = 0;
            foreach (var segment in _activeSegments)
            {
                maxY = Mathf.Max(maxY, segment.bottomY);
            }
            
            contentContainer.sizeDelta = new Vector2(contentContainer.sizeDelta.x, maxY);
        }
        
        private void OnDestroy()
        {
            // Освобождаем ресурсы через AddressablesCache
            AddressablesCache.ReleaseAssets(ReleaseKey.MainMenu);
        }
    }
}