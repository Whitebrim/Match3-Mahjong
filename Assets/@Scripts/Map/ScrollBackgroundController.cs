using System;
using UnityEngine;
using UnityEngine.UI;

namespace Map
{
    [Serializable]
    public class ColorZone
    {
        public Color color = Color.white;
        public float height = 3000f;
        
        [HideInInspector] public float startHeight;
        [HideInInspector] public float endHeight;
    }

    [RequireComponent(typeof(RawImage))]
    public class ScrollBackgroundController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private InfiniteScroll infiniteScroll;
        [SerializeField] private RawImage backgroundImage;

        [Header("Color Zones")]
        [SerializeField] private ColorZone[] colorZones = new ColorZone[]
        {
            new ColorZone { color = new Color(0.2f, 0.3f, 0.5f), height = 3000f },
            new ColorZone { color = new Color(0.3f, 0.2f, 0.4f), height = 3000f },
            new ColorZone { color = new Color(0.4f, 0.3f, 0.2f), height = 3000f },
        };

        [Header("Vignette Settings")]
        [SerializeField] private float vignetteIntensity = 0.3f;
        [SerializeField] private float vignetteSoftness = 0.5f;
        
        [Header("Transition")]
        [SerializeField] private float transitionSpeed = 5f;

        private Material _backgroundMaterial;
        private Color _currentColor;
        private Color _targetColor;

        private static readonly int ColorTopId = Shader.PropertyToID("_ColorTop");
        private static readonly int ColorBottomId = Shader.PropertyToID("_ColorBottom");
        private static readonly int VignetteIntensityId = Shader.PropertyToID("_VignetteIntensity");
        private static readonly int VignetteSoftnessId = Shader.PropertyToID("_VignetteSoftness");

        private void Awake()
        {
            if (backgroundImage == null)
                backgroundImage = GetComponent<RawImage>();

            InitializeMaterial();
            CalculateZoneHeights();
        }

        private void OnEnable()
        {
            if (infiniteScroll != null)
            {
                infiniteScroll.OnScrollPositionChanged += OnScrollPositionChanged;
            }
        }

        private void OnDisable()
        {
            if (infiniteScroll != null)
            {
                infiniteScroll.OnScrollPositionChanged -= OnScrollPositionChanged;
            }
        }

        private void InitializeMaterial()
        {
            Shader shader = Shader.Find("UI/ScrollBackground");
            if (shader == null)
            {
                Debug.LogError("Shader 'UI/ScrollBackground' not found!");
                return;
            }

            _backgroundMaterial = new Material(shader);
            backgroundImage.material = _backgroundMaterial;

            _currentColor = colorZones.Length > 0 ? colorZones[0].color : Color.white;
            _targetColor = _currentColor;

            UpdateMaterialProperties();
        }

        private void CalculateZoneHeights()
        {
            float currentHeight = 0f;
            
            foreach (var zone in colorZones)
            {
                zone.startHeight = currentHeight;
                zone.endHeight = currentHeight + zone.height;
                currentHeight = zone.endHeight;
            }
        }

        private void OnScrollPositionChanged(float scrollPosition)
        {
            _targetColor = GetColorAtPosition(scrollPosition);
        }

        private Color GetColorAtPosition(float position)
        {
            if (colorZones.Length == 0)
                return Color.white;
            
            var totalHeight = colorZones[^1].endHeight;
            
            var cyclicPosition = position % totalHeight;
            if (cyclicPosition < 0)
                cyclicPosition += totalHeight;
            
            for (var i = 0; i < colorZones.Length; i++)
            {
                var zone = colorZones[i];
                
                if (cyclicPosition >= zone.startHeight && cyclicPosition < zone.endHeight)
                {
                    var nextZoneIndex = (i + 1) % colorZones.Length;
                    var nextColor = colorZones[nextZoneIndex].color;
                    
                    var t = (cyclicPosition - zone.startHeight) / zone.height;
                    return Color.Lerp(zone.color, nextColor, t);
                }
            }

            return colorZones[0].color;
        }

        private void Update()
        {
            if (_currentColor != _targetColor)
            {
                _currentColor = Color.Lerp(_currentColor, _targetColor, Time.deltaTime * transitionSpeed);
                UpdateMaterialProperties();
            }
        }

        private void UpdateMaterialProperties()
        {
            if (_backgroundMaterial == null)
                return;

            var topColor = _currentColor * 1.1f;
            var bottomColor = _currentColor * 0.9f;

            _backgroundMaterial.SetColor(ColorTopId, topColor);
            _backgroundMaterial.SetColor(ColorBottomId, bottomColor);
            _backgroundMaterial.SetFloat(VignetteIntensityId, vignetteIntensity);
            _backgroundMaterial.SetFloat(VignetteSoftnessId, vignetteSoftness);
        }

        public void SetVignetteIntensity(float intensity)
        {
            vignetteIntensity = Mathf.Clamp01(intensity);
            UpdateMaterialProperties();
        }

        public void SetVignetteSoftness(float softness)
        {
            vignetteSoftness = Mathf.Clamp01(softness);
            UpdateMaterialProperties();
        }

        private void OnDestroy()
        {
            if (_backgroundMaterial != null)
            {
                Destroy(_backgroundMaterial);
            }
        }

        #if UNITY_EDITOR
        private void OnValidate()
        {
            if (Application.isPlaying && _backgroundMaterial != null)
            {
                CalculateZoneHeights();
                UpdateMaterialProperties();
            }
        }
        #endif
    }
}