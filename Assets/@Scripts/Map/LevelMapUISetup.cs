using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Map
{
    /// <summary>
    /// Helper класс для настройки UI карты уровней в Unity Editor
    /// </summary>
    [RequireComponent(typeof(Canvas))]
    public class LevelMapUISetup : MonoBehaviour
    {
        [Header("Auto Setup")]
        [SerializeField] private bool autoSetupOnStart = true;
        
        [Header("References")]
        [SerializeField] private LevelMapController mapController;
        [SerializeField] private LevelMapConfig mapConfig;
        
        private void Start()
        {
            if (autoSetupOnStart)
            {
                SetupLevelMapUI();
            }
        }
        
        [ContextMenu("Setup Level Map UI")]
        public void SetupLevelMapUI()
        {
            // Получаем Canvas
            var canvas = GetComponent<Canvas>();
            if (canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            {
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            }
            
            // Создаем структуру UI
            CreateBackgroundGradient();
            CreateScrollView();
            CreateUIOverlay();
        }
        
        private void CreateBackgroundGradient()
        {
            // Создаем фоновое изображение с градиентом
            var bgObject = new GameObject("Background");
            bgObject.transform.SetParent(transform, false);
            
            var bgRect = bgObject.AddComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.sizeDelta = Vector2.zero;
            bgRect.anchoredPosition = Vector2.zero;
            
            var bgImage = bgObject.AddComponent<RawImage>();
            
            // Создаем материал с нашим шейдером
            var gradientMaterial = new Material(Shader.Find("UI/BiomeGradient"));
            if (gradientMaterial != null)
            {
                // Устанавливаем начальные цвета для первого биома
                if (mapConfig != null && mapConfig.biomes.Count > 0)
                {
                    var firstBiome = mapConfig.biomes[0];
                    gradientMaterial.SetColor("_TopColor1", firstBiome.topGradientColor);
                    gradientMaterial.SetColor("_BottomColor1", firstBiome.bottomGradientColor);
                    gradientMaterial.SetColor("_TopColor2", firstBiome.topGradientColor);
                    gradientMaterial.SetColor("_BottomColor2", firstBiome.bottomGradientColor);
                    gradientMaterial.SetFloat("_BiomeBlend", 0);
                    gradientMaterial.SetFloat("_EdgeDarkness", 0.3f);
                    gradientMaterial.SetFloat("_EdgeSoftness", 0.3f);
                }
                
                bgImage.material = gradientMaterial;
                
                // Сохраняем ссылку в контроллере
                if (mapController != null)
                {
                    var controllerType = mapController.GetType();
                    var materialField = controllerType.GetField("backgroundMaterial", 
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    materialField?.SetValue(mapController, gradientMaterial);
                }
            }
            
            // Создаем рендер текстуру для градиента
            var renderTexture = new RenderTexture(Screen.width, Screen.height, 0);
            bgImage.texture = renderTexture;
        }
        
        private void CreateScrollView()
        {
            // Создаем ScrollView
            var scrollObject = new GameObject("ScrollView");
            scrollObject.transform.SetParent(transform, false);
            
            var scrollRect = scrollObject.AddComponent<ScrollRect>();
            var scrollRectTransform = scrollObject.GetComponent<RectTransform>();
            scrollRectTransform.anchorMin = Vector2.zero;
            scrollRectTransform.anchorMax = Vector2.one;
            scrollRectTransform.sizeDelta = Vector2.zero;
            scrollRectTransform.anchoredPosition = Vector2.zero;
            
            // Viewport
            var viewport = new GameObject("Viewport");
            viewport.transform.SetParent(scrollObject.transform, false);
            
            var viewportRect = viewport.AddComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.sizeDelta = Vector2.zero;
            viewportRect.anchoredPosition = Vector2.zero;
            
            var viewportMask = viewport.AddComponent<Image>();
            viewportMask.color = new Color(1, 1, 1, 0);
            viewport.AddComponent<Mask>().showMaskGraphic = false;
            
            // Content
            var content = new GameObject("Content");
            content.transform.SetParent(viewport.transform, false);
            
            var contentRect = content.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 1);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.pivot = new Vector2(0.5f, 1);
            contentRect.sizeDelta = new Vector2(0, 5000); // Начальная высота
            contentRect.anchoredPosition = Vector2.zero;
            
            // Настройка ScrollRect
            scrollRect.content = contentRect;
            scrollRect.viewport = viewportRect;
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Elastic;
            scrollRect.elasticity = 0.1f;
            scrollRect.inertia = true;
            scrollRect.decelerationRate = 0.135f;
            scrollRect.scrollSensitivity = 30f;
            
            // Добавляем scrollbar если нужно
            CreateScrollbar(scrollObject, scrollRect);
            
            // Сохраняем ссылки в контроллере
            if (mapController != null)
            {
                var controllerType = mapController.GetType();
                
                var scrollField = controllerType.GetField("scrollRect",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                scrollField?.SetValue(mapController, scrollRect);
                
                var contentField = controllerType.GetField("contentContainer",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                contentField?.SetValue(mapController, contentRect);
            }
        }
        
        private void CreateScrollbar(GameObject scrollView, ScrollRect scrollRect)
        {
            // Создаем вертикальный scrollbar
            var scrollbarObject = new GameObject("Scrollbar Vertical");
            scrollbarObject.transform.SetParent(scrollView.transform, false);
            
            var scrollbarRect = scrollbarObject.AddComponent<RectTransform>();
            scrollbarRect.anchorMin = new Vector2(1, 0);
            scrollbarRect.anchorMax = new Vector2(1, 1);
            scrollbarRect.pivot = new Vector2(1, 0.5f);
            scrollbarRect.sizeDelta = new Vector2(20, 0);
            scrollbarRect.anchoredPosition = new Vector2(-10, 0);
            
            var scrollbarImage = scrollbarObject.AddComponent<Image>();
            scrollbarImage.color = new Color(0.2f, 0.2f, 0.2f, 0.3f);
            
            var scrollbar = scrollbarObject.AddComponent<Scrollbar>();
            scrollbar.direction = Scrollbar.Direction.BottomToTop;
            
            // Sliding Area
            var slidingArea = new GameObject("Sliding Area");
            slidingArea.transform.SetParent(scrollbarObject.transform, false);
            
            var slidingRect = slidingArea.AddComponent<RectTransform>();
            slidingRect.anchorMin = Vector2.zero;
            slidingRect.anchorMax = Vector2.one;
            slidingRect.sizeDelta = new Vector2(-20, -20);
            slidingRect.anchoredPosition = Vector2.zero;
            
            // Handle
            var handle = new GameObject("Handle");
            handle.transform.SetParent(slidingArea.transform, false);
            
            var handleRect = handle.AddComponent<RectTransform>();
            handleRect.sizeDelta = new Vector2(20, 20);
            
            var handleImage = handle.AddComponent<Image>();
            handleImage.color = new Color(0.5f, 0.5f, 0.5f, 0.8f);
            
            scrollbar.targetGraphic = handleImage;
            scrollbar.handleRect = handleRect;
            
            scrollRect.verticalScrollbar = scrollbar;
            scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport;
            scrollRect.verticalScrollbarSpacing = -3;
        }
        
        private void CreateUIOverlay()
        {
            // Создаем верхнюю панель
            CreateTopPanel();
            
            // Создаем нижнюю панель с кнопками
            CreateBottomPanel();
        }
        
        private void CreateTopPanel()
        {
            var topPanel = new GameObject("TopPanel");
            topPanel.transform.SetParent(transform, false);
            
            var topRect = topPanel.AddComponent<RectTransform>();
            topRect.anchorMin = new Vector2(0, 1);
            topRect.anchorMax = new Vector2(1, 1);
            topRect.pivot = new Vector2(0.5f, 1);
            topRect.sizeDelta = new Vector2(0, 100);
            topRect.anchoredPosition = Vector2.zero;
            
            var topImage = topPanel.AddComponent<Image>();
            topImage.color = new Color(0, 0, 0, 0.3f);
            
            // Добавляем элементы UI
            CreateStarsDisplay(topPanel);
            CreateLevelCounter(topPanel);
        }
        
        private void CreateStarsDisplay(GameObject parent)
        {
            var starsObject = new GameObject("Stars");
            starsObject.transform.SetParent(parent.transform, false);
            
            var starsRect = starsObject.AddComponent<RectTransform>();
            starsRect.anchorMin = new Vector2(0, 0.5f);
            starsRect.anchorMax = new Vector2(0, 0.5f);
            starsRect.pivot = new Vector2(0, 0.5f);
            starsRect.sizeDelta = new Vector2(200, 50);
            starsRect.anchoredPosition = new Vector2(20, 0);
            
            var starsText = starsObject.AddComponent<TextMeshProUGUI>();
            starsText.text = "Stars 0";
            starsText.fontSize = 36;
            starsText.alignment = TextAlignmentOptions.MidlineLeft;
        }
        
        private void CreateLevelCounter(GameObject parent)
        {
            var levelObject = new GameObject("LevelCounter");
            levelObject.transform.SetParent(parent.transform, false);
            
            var levelRect = levelObject.AddComponent<RectTransform>();
            levelRect.anchorMin = new Vector2(1, 0.5f);
            levelRect.anchorMax = new Vector2(1, 0.5f);
            levelRect.pivot = new Vector2(1, 0.5f);
            levelRect.sizeDelta = new Vector2(300, 50);
            levelRect.anchoredPosition = new Vector2(-20, 0);
            
            var levelText = levelObject.AddComponent<TextMeshProUGUI>();
            levelText.text = "Level 1 / 100";
            levelText.fontSize = 28;
            levelText.alignment = TextAlignmentOptions.MidlineRight;
        }
        
        private void CreateBottomPanel()
        {
            var bottomPanel = new GameObject("BottomPanel");
            bottomPanel.transform.SetParent(transform, false);
            
            var bottomRect = bottomPanel.AddComponent<RectTransform>();
            bottomRect.anchorMin = new Vector2(0, 0);
            bottomRect.anchorMax = new Vector2(1, 0);
            bottomRect.pivot = new Vector2(0.5f, 0);
            bottomRect.sizeDelta = new Vector2(0, 120);
            bottomRect.anchoredPosition = Vector2.zero;
            
            var bottomImage = bottomPanel.AddComponent<Image>();
            bottomImage.color = new Color(0, 0, 0, 0.3f);
            
            // Добавляем кнопки
            CreateBackButton(bottomPanel);
            CreateShopButton(bottomPanel);
        }
        
        private void CreateBackButton(GameObject parent)
        {
            var buttonObject = new GameObject("BackButton");
            buttonObject.transform.SetParent(parent.transform, false);
            
            var buttonRect = buttonObject.AddComponent<RectTransform>();
            buttonRect.anchorMin = new Vector2(0, 0.5f);
            buttonRect.anchorMax = new Vector2(0, 0.5f);
            buttonRect.pivot = new Vector2(0, 0.5f);
            buttonRect.sizeDelta = new Vector2(150, 60);
            buttonRect.anchoredPosition = new Vector2(20, 0);
            
            var button = buttonObject.AddComponent<Button>();
            var buttonImage = buttonObject.AddComponent<Image>();
            buttonImage.color = new Color(0.2f, 0.5f, 0.8f);
            
            var buttonText = new GameObject("Text");
            buttonText.transform.SetParent(buttonObject.transform, false);
            
            var textRect = buttonText.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;
            textRect.anchoredPosition = Vector2.zero;
            
            var text = buttonText.AddComponent<TextMeshProUGUI>();
            text.text = "Back";
            text.fontSize = 24;
            text.alignment = TextAlignmentOptions.Center;
        }
        
        private void CreateShopButton(GameObject parent)
        {
            var buttonObject = new GameObject("ShopButton");
            buttonObject.transform.SetParent(parent.transform, false);
            
            var buttonRect = buttonObject.AddComponent<RectTransform>();
            buttonRect.anchorMin = new Vector2(1, 0.5f);
            buttonRect.anchorMax = new Vector2(1, 0.5f);
            buttonRect.pivot = new Vector2(1, 0.5f);
            buttonRect.sizeDelta = new Vector2(150, 60);
            buttonRect.anchoredPosition = new Vector2(-20, 0);
            
            var button = buttonObject.AddComponent<Button>();
            var buttonImage = buttonObject.AddComponent<Image>();
            buttonImage.color = new Color(0.8f, 0.6f, 0.2f);
            
            var buttonText = new GameObject("Text");
            buttonText.transform.SetParent(buttonObject.transform, false);
            
            var textRect = buttonText.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;
            textRect.anchoredPosition = Vector2.zero;
            
            var text = buttonText.AddComponent<TextMeshProUGUI>();
            text.text = "Shop";
            text.fontSize = 24;
            text.alignment = TextAlignmentOptions.Center;
        }
    }
}