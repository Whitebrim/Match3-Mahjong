using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Pool;
using VContainer;
using VContainer.Unity;

namespace Map
{
    public class InfiniteScroll : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [Inject] private readonly IObjectResolver _resolver;
        
        [Header("Prefab Settings")] [SerializeField]
        private GameObject itemPrefab;

        [SerializeField] private RectTransform content;

        [Header("Scroll Settings")] [SerializeField]
        private float itemHeight = 100f;

        [SerializeField] private float spacing = 0f;
        [SerializeField] private int bufferCount = 1;

        [Header("Scroll State")] [SerializeField]
        private float currentScrollPosition = 0f;

        public float CurrentScrollPosition => currentScrollPosition;
        
        public event Action<float> OnScrollPositionChanged;
        public event Action<long> OnItemIndexChanged;
        
        private RectTransform viewport;
        private ObjectPool<GameObject> itemPool;
        private List<ScrollItem> activeItems = new List<ScrollItem>();
        private float viewportHeight;
        private long visibleItemCount;
        private long totalItemCount = long.MaxValue;
        private long currentStartIndex;
        
        private bool isDragging = false;
        private float dragStartY;
        private float contentStartY;
        private Vector2 velocity = Vector2.zero;
        private float deceleration = 0.95f;
        
        private float scrollSensitivity = 1f;
        private float inertia = 0.9f;

        private void Awake()
        {
            viewport = GetComponent<RectTransform>();
            if (content == null)
            {
                GameObject contentGO = new GameObject("Content");
                contentGO.transform.SetParent(transform, false);
                content = contentGO.AddComponent<RectTransform>();
                content.anchorMin = new Vector2(0, 0);
                content.anchorMax = new Vector2(1, 1);
                content.sizeDelta = Vector2.zero;
                content.anchoredPosition = Vector2.zero;
            }

            InitializePool();
        }

        private void Start()
        {
            CalculateVisibleItems();
            PopulateItems();
        }

        private void InitializePool()
        {
            itemPool = new ObjectPool<GameObject>(
                createFunc: () => CreatePooledItem(),
                actionOnGet: (obj) => OnGetFromPool(obj),
                actionOnRelease: (obj) => OnReleaseToPool(obj),
                actionOnDestroy: (obj) => Destroy(obj),
                collectionCheck: false,
                defaultCapacity: 3,
                maxSize: 10
            );
        }

        private GameObject CreatePooledItem()
        {
            GameObject item = _resolver.Instantiate(itemPrefab, content);
            RectTransform rectTransform = item.GetComponent<RectTransform>();
            if (rectTransform == null)
                rectTransform = item.AddComponent<RectTransform>();
            
            rectTransform.anchorMin = new Vector2(0, 0);
            rectTransform.anchorMax = new Vector2(1, 0);
            rectTransform.pivot = new Vector2(0.5f, 0);
            rectTransform.sizeDelta = new Vector2(0, itemHeight);

            ScrollItem scrollItem = item.GetComponent<ScrollItem>();
            if (scrollItem == null)
                scrollItem = item.AddComponent<ScrollItem>();

            return item;
        }

        private void OnGetFromPool(GameObject obj)
        {
            obj.SetActive(true);
        }

        private void OnReleaseToPool(GameObject obj)
        {
            obj.SetActive(false);
        }

        private void CalculateVisibleItems()
        {
            viewportHeight = viewport.rect.height;
            visibleItemCount = (long)(Math.Ceiling(viewportHeight / (itemHeight + spacing)) + bufferCount * 2);
        }

        private void PopulateItems()
        {
            foreach (var item in activeItems)
            {
                itemPool.Release(item.gameObject);
            }

            activeItems.Clear();
            
            for (int i = 0; i < visibleItemCount && i < totalItemCount; i++)
            {
                GameObject itemGO = itemPool.Get();
                ScrollItem item = itemGO.GetComponent<ScrollItem>();
                item.UpdateData(currentStartIndex + i);

                RectTransform rectTransform = itemGO.GetComponent<RectTransform>();
                float yPos = i * (itemHeight + spacing);
                rectTransform.anchoredPosition = new Vector2(0, yPos);

                activeItems.Add(item);
            }
        }

        private void Update()
        {
            HandleScrollInput();

            if (!isDragging && velocity.magnitude > 0.01f)
            {
                currentScrollPosition += velocity.y * Time.deltaTime;
                velocity *= deceleration;

                UpdateScroll();
            }
        }

        private void HandleScrollInput()
        {
            float scrollWheel = Input.GetAxis("Mouse ScrollWheel");
            if (scrollWheel != 0 && !isDragging)
            {
                currentScrollPosition += scrollWheel * 500f * scrollSensitivity;
                UpdateScroll();
            }
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            isDragging = true;
            dragStartY = eventData.position.y;
            contentStartY = currentScrollPosition;
            velocity = Vector2.zero;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!isDragging) return;

            float deltaY = eventData.position.y - dragStartY;
            float newScrollPosition = contentStartY - deltaY * scrollSensitivity;
            
            velocity = new Vector2(0, (newScrollPosition - currentScrollPosition) / Time.deltaTime * inertia);

            currentScrollPosition = newScrollPosition;
            UpdateScroll();
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            isDragging = false;
        }

        private void UpdateScroll()
        {
            if (currentScrollPosition < 0)
            {
                currentScrollPosition = 0;
                velocity = Vector2.zero;
            }
            
            content.anchoredPosition = new Vector2(0, -currentScrollPosition);
            
            OnScrollPositionChanged?.Invoke(currentScrollPosition);
            
            CheckItemRecycling();
        }

        private void CheckItemRecycling()
        {
            if (activeItems.Count == 0) return;

            float scrollOffset = currentScrollPosition;
            int newStartIndex = Mathf.Max(0, Mathf.FloorToInt(scrollOffset / (itemHeight + spacing)) - bufferCount);

            if (newStartIndex != currentStartIndex)
            {
                long diff = newStartIndex - currentStartIndex;

                if (Mathf.Abs(diff) >= visibleItemCount)
                {
                    currentStartIndex = newStartIndex;
                    PopulateItems();
                }
                else if (diff > 0)
                {
                    RecycleItemsUp(diff);
                }
                else
                {
                    RecycleItemsDown(-diff);
                }

                currentStartIndex = newStartIndex;
                OnItemIndexChanged?.Invoke(currentStartIndex);
            }
        }

        private void RecycleItemsUp(long count)
        {
            count = Math.Min(count, activeItems.Count);

            for (int i = 0; i < count; i++)
            {
                if (activeItems.Count == 0) break;

                ScrollItem itemToRecycle = activeItems[0];
                activeItems.RemoveAt(0);

                long newIndex = currentStartIndex + visibleItemCount + i;
                if (newIndex < totalItemCount)
                {
                    itemToRecycle.UpdateData(newIndex);

                    RectTransform rectTransform = itemToRecycle.GetComponent<RectTransform>();
                    float yPos = newIndex * (itemHeight + spacing);
                    rectTransform.anchoredPosition = new Vector2(0, yPos);

                    activeItems.Add(itemToRecycle);
                }
                else
                {
                    itemPool.Release(itemToRecycle.gameObject);
                }
            }
        }

        private void RecycleItemsDown(long count)
        {
            count = Math.Min(count, activeItems.Count);

            for (int i = 0; i < count; i++)
            {
                if (activeItems.Count == 0) break;

                ScrollItem itemToRecycle = activeItems[^1];
                activeItems.RemoveAt(activeItems.Count - 1);

                long newIndex = currentStartIndex - i - 1;
                if (newIndex >= 0)
                {
                    itemToRecycle.UpdateData(newIndex);

                    RectTransform rectTransform = itemToRecycle.GetComponent<RectTransform>();
                    float yPos = newIndex * (itemHeight + spacing);
                    rectTransform.anchoredPosition = new Vector2(0, yPos);

                    activeItems.Insert(0, itemToRecycle);
                }
                else
                {
                    itemPool.Release(itemToRecycle.gameObject);
                }
            }
        }
        
        public void ScrollToPosition(float position)
        {
            currentScrollPosition = Mathf.Max(0, position);
            UpdateScroll();
        }

        public void ScrollToIndex(int index)
        {
            float targetPosition = index * (itemHeight + spacing);
            ScrollToPosition(targetPosition);
        }

        public void SetScrollSensitivity(float sensitivity)
        {
            scrollSensitivity = Mathf.Clamp(sensitivity, 0.1f, 3f);
        }

        public void SetInertia(float inertiaValue)
        {
            inertia = Mathf.Clamp01(inertiaValue);
        }

        private void OnDestroy()
        {
            itemPool?.Dispose();
        }
    }
}