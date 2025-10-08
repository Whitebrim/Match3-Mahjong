using Core.Infrastructure.StateMachine;
using Core.Infrastructure.StateMachine.States;
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
        
        private long _levelNumber;
        private bool _isUnlocked;
        private int _starsEarned;

        public void UpdateData(long level)
        {
            _levelNumber = level;
            levelNumberText.text = level.ToString();
        }
        
        public void StartLevel()
        {
            if (_stateMachine.CurrentState is SelectLevelState state)
            {
                _ = state.EnterLevel(_levelNumber);
            }
            else
            {
                _stateMachine.Enter<SelectLevelState>();
                StartLevel();
            }
        }
    }
}