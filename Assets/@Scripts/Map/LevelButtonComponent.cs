using Core.Infrastructure.StateMachine;
using Core.Infrastructure.StateMachine.States;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace Map
{
    public class LevelButton : MonoBehaviour
    {
        [Inject] private readonly GameStateMachine _stateMachine;
        
        [Header("UI References")]
        [SerializeField] private Button button;
        [SerializeField] private TextMeshProUGUI levelNumberText;
        [SerializeField] private Image levelIcon;
        [SerializeField] private Image lockIcon;
        [SerializeField] private Image[] starIcons = new Image[3];
        [SerializeField] private GameObject completedOverlay;
        
        [Header("Visual Settings")]
        [SerializeField] private Color unlockedColor = Color.white;
        [SerializeField] private Color lockedColor = new Color(0.5f, 0.5f, 0.5f, 0.8f);
        [SerializeField] private Color completedColor = new Color(0.8f, 1f, 0.8f, 1f);
        
        [Header("Animation")]
        [SerializeField] private float hoverScale = 1.1f;
        [SerializeField] private float clickScale = 0.95f;
        [SerializeField] private float animationDuration = 0.2f;
        
        private int _levelNumber;
        private bool _isUnlocked;
        private int _starsEarned;
        private RectTransform _rectTransform;
        private Vector3 _originalScale;
        
        public int LevelNumber => _levelNumber;
        public bool IsUnlocked => _isUnlocked;
        
        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
            _originalScale = transform.localScale;
            
            button.onClick.AddListener(OnLevelButtonClick);
            
            // Добавляем анимации при наведении
            var eventTrigger = gameObject.AddComponent<UnityEngine.EventSystems.EventTrigger>();
            
            var pointerEnter = new UnityEngine.EventSystems.EventTrigger.Entry
            {
                eventID = UnityEngine.EventSystems.EventTriggerType.PointerEnter
            };
            pointerEnter.callback.AddListener(_ => OnPointerEnter());
            eventTrigger.triggers.Add(pointerEnter);
            
            var pointerExit = new UnityEngine.EventSystems.EventTrigger.Entry
            {
                eventID = UnityEngine.EventSystems.EventTriggerType.PointerExit
            };
            pointerExit.callback.AddListener(_ => OnPointerExit());
            eventTrigger.triggers.Add(pointerExit);
        }
        
        public void Initialize(int levelNumber, bool isUnlocked, int starsEarned = 0)
        {
            _levelNumber = levelNumber;
            _isUnlocked = isUnlocked;
            _starsEarned = Mathf.Clamp(starsEarned, 0, 3);
            
            UpdateVisuals();
        }
        
        private void UpdateVisuals()
        {
            // Устанавливаем номер уровня
            if (levelNumberText != null)
            {
                levelNumberText.text = _levelNumber.ToString();
            }
            
            // Настраиваем доступность кнопки
            button.interactable = _isUnlocked;
            
            // Показываем/скрываем замок
            if (lockIcon != null)
            {
                lockIcon.gameObject.SetActive(!_isUnlocked);
            }
            
            // Настраиваем цвет
            if (levelIcon != null)
            {
                if (_starsEarned > 0)
                {
                    levelIcon.color = completedColor;
                }
                else if (_isUnlocked)
                {
                    levelIcon.color = unlockedColor;
                }
                else
                {
                    levelIcon.color = lockedColor;
                }
            }
            
            // Показываем оверлей для пройденных уровней
            if (completedOverlay != null)
            {
                completedOverlay.SetActive(_starsEarned > 0);
            }
            
            // Обновляем звезды
            UpdateStars();
        }
        
        private void UpdateStars()
        {
            if (starIcons == null || starIcons.Length == 0) return;
            
            for (int i = 0; i < starIcons.Length; i++)
            {
                if (starIcons[i] != null)
                {
                    bool earned = i < _starsEarned;
                    starIcons[i].gameObject.SetActive(_isUnlocked && _starsEarned > 0);
                    
                    if (earned)
                    {
                        starIcons[i].color = Color.yellow;
                    }
                    else
                    {
                        starIcons[i].color = new Color(0.3f, 0.3f, 0.3f, 0.5f);
                    }
                }
            }
        }
        
        private void OnLevelButtonClick()
        {
            if (!_isUnlocked) return;
            
            // Анимация нажатия
            transform.DOScale(_originalScale * clickScale, animationDuration * 0.5f)
                .SetEase(Ease.OutQuad)
                .OnComplete(() =>
                {
                    transform.DOScale(_originalScale, animationDuration * 0.5f)
                        .SetEase(Ease.OutBack);
                });
            
            // Запускаем уровень
            LoadLevel();
        }
        
        private void LoadLevel()
        {
            // Переходим в состояние игры с выбранным уровнем
            _stateMachine.Enter<GameState, ulong>((ulong)_levelNumber);
        }
        
        private void OnPointerEnter()
        {
            if (!_isUnlocked) return;
            
            transform.DOScale(_originalScale * hoverScale, animationDuration)
                .SetEase(Ease.OutBack);
        }
        
        private void OnPointerExit()
        {
            transform.DOScale(_originalScale, animationDuration)
                .SetEase(Ease.OutQuad);
        }
        
        public void PlayUnlockAnimation()
        {
            // Анимация разблокировки уровня
            transform.localScale = Vector3.zero;
            transform.DOScale(_originalScale, 0.5f)
                .SetEase(Ease.OutBounce);
            
            // Particle effect можно добавить здесь
        }
        
        public void SetStars(int count, bool animate = false)
        {
            _starsEarned = Mathf.Clamp(count, 0, 3);
            
            if (animate)
            {
                // Анимация появления звезд
                for (int i = 0; i < _starsEarned; i++)
                {
                    if (starIcons[i] != null)
                    {
                        int index = i;
                        DOVirtual.DelayedCall(i * 0.2f, () =>
                        {
                            starIcons[index].transform.localScale = Vector3.zero;
                            starIcons[index].gameObject.SetActive(true);
                            starIcons[index].transform.DOScale(1f, 0.3f)
                                .SetEase(Ease.OutBounce);
                        });
                    }
                }
            }
            else
            {
                UpdateStars();
            }
        }
        
        private void OnDestroy()
        {
            DOTween.Kill(transform);
        }
    }
}