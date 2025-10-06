using System;
using System.Collections.Generic;
using Core.Services.AssetManagement;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using Utils.Extensions;

namespace Map
{
    public class MapObjectPool : MonoBehaviour
    {
        [Serializable]
        public class PoolConfig
        {
            public string poolKey;
            public AssetReferenceT<GameObject> prefabReference;
            public int initialSize = 10;
            public int maxSize = 50;
            public bool autoExpand = true;
            public string releaseKey = ReleaseKey.MainMenu;
        }
        
        [SerializeField] private List<PoolConfig> poolConfigs = new();
        [SerializeField] private Transform poolContainer;
        
        private readonly Dictionary<string, ObjectPool> _pools = new();
        
        private class ObjectPool
        {
            public readonly Queue<GameObject> Available = new();
            public readonly HashSet<GameObject> InUse = new();
            public GameObject Prefab;
            public Transform Container;
            public int MaxSize;
            public bool AutoExpand;
            public string ReleaseKey;
            
            public int TotalCount => Available.Count + InUse.Count;
        }
        
        private async void Start()
        {
            await InitializePoolsAsync();
        }
        
        private async UniTask InitializePoolsAsync()
        {
            if (poolContainer == null)
            {
                poolContainer = new GameObject("PoolContainer").transform;
                poolContainer.SetParent(transform);
            }
            
            foreach (var config in poolConfigs)
            {
                await CreatePool(config);
            }
        }
        
        private async UniTask CreatePool(PoolConfig config)
        {
            if (_pools.ContainsKey(config.poolKey))
            {
                Debug.LogWarning($"Pool with key {config.poolKey} already exists");
                return;
            }
            
            // Загружаем префаб используя LoadAndCacheAsync
            GameObject prefab = await config.prefabReference.LoadAndCacheAsync<GameObject>(config.releaseKey);
            
            if (prefab == null)
            {
                Debug.LogError($"Failed to load prefab for pool {config.poolKey}");
                return;
            }
            
            // Создаем контейнер для пула
            var container = new GameObject($"Pool_{config.poolKey}").transform;
            container.SetParent(poolContainer);
            
            // Создаем пул
            var pool = new ObjectPool
            {
                Prefab = prefab,
                Container = container,
                MaxSize = config.maxSize,
                AutoExpand = config.autoExpand,
                ReleaseKey = config.releaseKey
            };
            
            // Предварительно создаем объекты
            for (int i = 0; i < config.initialSize; i++)
            {
                var obj = CreatePoolObject(pool);
                pool.Available.Enqueue(obj);
            }
            
            _pools[config.poolKey] = pool;
        }
        
        private GameObject CreatePoolObject(ObjectPool pool)
        {
            var obj = Instantiate(pool.Prefab, pool.Container);
            obj.SetActive(false);
            
            // Добавляем компонент для автоматического возврата в пул
            var poolable = obj.AddComponent<PoolableObject>();
            poolable.OnReturnToPool = () => ReturnToPool(GetPoolKeyForObject(obj), obj);
            
            return obj;
        }
        
        public GameObject Get(string poolKey, Transform parent = null)
        {
            if (!_pools.TryGetValue(poolKey, out var pool))
            {
                Debug.LogError($"Pool with key {poolKey} not found");
                return null;
            }
            
            GameObject obj = null;
            
            if (pool.Available.Count > 0)
            {
                obj = pool.Available.Dequeue();
            }
            else if (pool.AutoExpand || pool.TotalCount < pool.MaxSize)
            {
                obj = CreatePoolObject(pool);
            }
            else
            {
                Debug.LogWarning($"Pool {poolKey} has reached max size");
                return null;
            }
            
            if (obj != null)
            {
                pool.InUse.Add(obj);
                obj.transform.SetParent(parent ?? pool.Container);
                obj.SetActive(true);
                
                // Сбрасываем состояние объекта
                ResetObject(obj);
            }
            
            return obj;
        }
        
        public T Get<T>(string poolKey, Transform parent = null) where T : Component
        {
            var obj = Get(poolKey, parent);
            return obj?.GetComponent<T>();
        }
        
        public void ReturnToPool(string poolKey, GameObject obj)
        {
            if (!_pools.TryGetValue(poolKey, out var pool))
            {
                Debug.LogError($"Pool with key {poolKey} not found");
                Destroy(obj);
                return;
            }
            
            if (!pool.InUse.Contains(obj))
            {
                Debug.LogWarning($"Object {obj.name} is not from pool {poolKey}");
                return;
            }
            
            pool.InUse.Remove(obj);
            obj.SetActive(false);
            obj.transform.SetParent(pool.Container);
            pool.Available.Enqueue(obj);
        }
        
        public void ReturnAll(string poolKey)
        {
            if (!_pools.TryGetValue(poolKey, out var pool))
            {
                Debug.LogError($"Pool with key {poolKey} not found");
                return;
            }
            
            var toReturn = new List<GameObject>(pool.InUse);
            foreach (var obj in toReturn)
            {
                ReturnToPool(poolKey, obj);
            }
        }
        
        public void ReturnAllPools()
        {
            foreach (var poolKey in _pools.Keys)
            {
                ReturnAll(poolKey);
            }
        }
        
        private void ResetObject(GameObject obj)
        {
            // Сбрасываем позицию и поворот
            var rectTransform = obj.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                rectTransform.anchoredPosition = Vector2.zero;
                rectTransform.localRotation = Quaternion.identity;
                rectTransform.localScale = Vector3.one;
            }
            else
            {
                obj.transform.localPosition = Vector3.zero;
                obj.transform.localRotation = Quaternion.identity;
                obj.transform.localScale = Vector3.one;
            }
            
            // Вызываем метод сброса если есть
            var poolable = obj.GetComponent<PoolableObject>();
            poolable?.ResetState();
        }
        
        private string GetPoolKeyForObject(GameObject obj)
        {
            foreach (var kvp in _pools)
            {
                if (kvp.Value.InUse.Contains(obj) || kvp.Value.Available.Contains(obj))
                {
                    return kvp.Key;
                }
            }
            return null;
        }
        
        public void PrewarmPool(string poolKey, int count)
        {
            if (!_pools.TryGetValue(poolKey, out var pool))
            {
                Debug.LogError($"Pool with key {poolKey} not found");
                return;
            }
            
            int toCreate = Mathf.Min(count - pool.TotalCount, pool.MaxSize - pool.TotalCount);
            
            for (int i = 0; i < toCreate; i++)
            {
                var obj = CreatePoolObject(pool);
                pool.Available.Enqueue(obj);
            }
        }
        
        public int GetAvailableCount(string poolKey)
        {
            return _pools.TryGetValue(poolKey, out var pool) ? pool.Available.Count : 0;
        }
        
        public int GetInUseCount(string poolKey)
        {
            return _pools.TryGetValue(poolKey, out var pool) ? pool.InUse.Count : 0;
        }
        
        private void OnDestroy()
        {
            // Очищаем все пулы
            foreach (var pool in _pools.Values)
            {
                // Все объекты уничтожаются вместе с GameObject
                // Ресурсы освобождаются через AddressablesCache
                if (!string.IsNullOrEmpty(pool.ReleaseKey))
                {
                    AddressablesCache.ReleaseAssets(pool.ReleaseKey);
                }
            }
        }
    }
    
    // Вспомогательный компонент для объектов в пуле
    public class PoolableObject : MonoBehaviour
    {
        public Action OnReturnToPool;
        
        public virtual void ResetState()
        {
            // Переопределяется в наследниках для специфичного сброса состояния
        }
        
        public void ReturnToPool()
        {
            OnReturnToPool?.Invoke();
        }
    }
}