using System;
using System.Collections.Generic;
using UnityEngine;

namespace Map
{
    public class LevelProgressManager : MonoBehaviour
    {
        private const string PROGRESS_KEY = "LevelProgress";
        private const string CURRENT_LEVEL_KEY = "CurrentLevel";
        private const string TOTAL_STARS_KEY = "TotalStars";
        
        private static LevelProgressManager _instance;
        public static LevelProgressManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<LevelProgressManager>();
                    if (_instance == null)
                    {
                        var go = new GameObject("LevelProgressManager");
                        _instance = go.AddComponent<LevelProgressManager>();
                        DontDestroyOnLoad(go);
                    }
                }
                return _instance;
            }
        }
        
        private Dictionary<int, LevelProgress> _levelProgress = new();
        private int _maxUnlockedLevel = 1;
        private int _totalStarsEarned = 0;
        
        [Serializable]
        public class LevelProgress
        {
            public int levelNumber;
            public bool isCompleted;
            public int starsEarned;
            public float bestTime;
            public int bestScore;
            
            public LevelProgress(int level)
            {
                levelNumber = level;
                isCompleted = false;
                starsEarned = 0;
                bestTime = float.MaxValue;
                bestScore = 0;
            }
        }
        
        [Serializable]
        private class SaveData
        {
            public List<LevelProgress> levels = new();
            public int maxUnlockedLevel = 1;
            public int totalStarsEarned = 0;
        }
        
        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            
            _instance = this;
            DontDestroyOnLoad(gameObject);
            
            LoadProgress();
        }
        
        public bool IsLevelUnlocked(int levelNumber)
        {
            return levelNumber <= _maxUnlockedLevel;
        }
        
        public LevelProgress GetLevelProgress(int levelNumber)
        {
            if (!_levelProgress.TryGetValue(levelNumber, out var progress))
            {
                progress = new LevelProgress(levelNumber);
                _levelProgress[levelNumber] = progress;
            }
            return progress;
        }
        
        public int GetLevelStars(int levelNumber)
        {
            return GetLevelProgress(levelNumber).starsEarned;
        }
        
        public void CompleteLevel(int levelNumber, int starsEarned, float time, int score)
        {
            var progress = GetLevelProgress(levelNumber);
            
            bool isFirstCompletion = !progress.isCompleted;
            progress.isCompleted = true;
            
            // Обновляем звезды
            int previousStars = progress.starsEarned;
            progress.starsEarned = Mathf.Max(progress.starsEarned, starsEarned);
            int starsGained = progress.starsEarned - previousStars;
            
            if (starsGained > 0)
            {
                _totalStarsEarned += starsGained;
            }
            
            // Обновляем лучшие результаты
            progress.bestTime = Mathf.Min(progress.bestTime, time);
            progress.bestScore = Mathf.Max(progress.bestScore, score);
            
            // Разблокируем следующий уровень
            if (levelNumber >= _maxUnlockedLevel)
            {
                _maxUnlockedLevel = levelNumber + 1;
                OnLevelUnlocked?.Invoke(_maxUnlockedLevel);
            }
            
            // Сохраняем прогресс
            SaveProgress();
            
            // Вызываем события
            OnLevelCompleted?.Invoke(levelNumber, starsEarned);
            if (isFirstCompletion)
            {
                OnLevelFirstCompletion?.Invoke(levelNumber);
            }
        }
        
        public int GetMaxUnlockedLevel()
        {
            return _maxUnlockedLevel;
        }
        
        public int GetTotalStarsEarned()
        {
            return _totalStarsEarned;
        }
        
        public int GetTotalStarsInRange(int startLevel, int endLevel)
        {
            int total = 0;
            for (int i = startLevel; i <= endLevel; i++)
            {
                total += GetLevelStars(i);
            }
            return total;
        }
        
        public float GetCompletionPercentage(int maxLevel)
        {
            if (maxLevel <= 0) return 0;
            
            int completedLevels = 0;
            for (int i = 1; i <= maxLevel; i++)
            {
                if (GetLevelProgress(i).isCompleted)
                {
                    completedLevels++;
                }
            }
            
            return (float)completedLevels / maxLevel * 100f;
        }
        
        private void SaveProgress()
        {
            var saveData = new SaveData
            {
                maxUnlockedLevel = _maxUnlockedLevel,
                totalStarsEarned = _totalStarsEarned,
                levels = new List<LevelProgress>(_levelProgress.Values)
            };
            
            string json = JsonUtility.ToJson(saveData);
            PlayerPrefs.SetString(PROGRESS_KEY, json);
            PlayerPrefs.SetInt(CURRENT_LEVEL_KEY, _maxUnlockedLevel);
            PlayerPrefs.SetInt(TOTAL_STARS_KEY, _totalStarsEarned);
            PlayerPrefs.Save();
        }
        
        private void LoadProgress()
        {
            string json = PlayerPrefs.GetString(PROGRESS_KEY, "");
            
            if (!string.IsNullOrEmpty(json))
            {
                try
                {
                    var saveData = JsonUtility.FromJson<SaveData>(json);
                    
                    _maxUnlockedLevel = saveData.maxUnlockedLevel;
                    _totalStarsEarned = saveData.totalStarsEarned;
                    
                    _levelProgress.Clear();
                    foreach (var level in saveData.levels)
                    {
                        _levelProgress[level.levelNumber] = level;
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"Failed to load progress: {e.Message}");
                    ResetProgress();
                }
            }
            else
            {
                // Fallback на старые ключи
                _maxUnlockedLevel = PlayerPrefs.GetInt(CURRENT_LEVEL_KEY, 1);
                _totalStarsEarned = PlayerPrefs.GetInt(TOTAL_STARS_KEY, 0);
            }
        }
        
        public void ResetProgress()
        {
            _levelProgress.Clear();
            _maxUnlockedLevel = 1;
            _totalStarsEarned = 0;
            SaveProgress();
            
            OnProgressReset?.Invoke();
        }
        
        // События
        public static event Action<int, int> OnLevelCompleted; // levelNumber, stars
        public static event Action<int> OnLevelFirstCompletion; // levelNumber
        public static event Action<int> OnLevelUnlocked; // levelNumber
        public static event Action OnProgressReset;
        
        // Cheats для тестирования
        [ContextMenu("Unlock All Levels")]
        public void UnlockAllLevels(int count = 100)
        {
            _maxUnlockedLevel = count;
            SaveProgress();
        }
        
        [ContextMenu("Complete All Levels With 3 Stars")]
        public void CompleteAllLevelsWithMaxStars(int count = 100)
        {
            for (int i = 1; i <= count; i++)
            {
                CompleteLevel(i, 3, UnityEngine.Random.Range(30f, 120f), UnityEngine.Random.Range(1000, 10000));
            }
        }
    }
}