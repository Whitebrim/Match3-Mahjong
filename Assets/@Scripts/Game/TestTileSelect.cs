using Game.Tiles;
using Solo.MOST_IN_ONE;
using UnityEngine;

namespace Game
{
    public class TestTileSelect : MonoBehaviour
    {
        [SerializeField] private FieldHandler fieldHandler;
        [SerializeField] private LayerMask tileLayer;
        [SerializeField] private Buffer buffer;
    
        private Camera _cam;
        private GameObject _selectedTile;
        private Vector3 _originalScale;
        private float _pressTime;
        private bool _isHolding;
        private bool _isPressed;
        private bool _isBlocked;
    
        void Start()
        {
            _cam = Camera.main;
        }
    
        void Update()
        {
            HandleInput();
        }
    
        void HandleInput()
        {
            bool inputDown = Input.GetMouseButtonDown(0);
            bool inputHeld = Input.GetMouseButton(0);
            bool inputUp = Input.GetMouseButtonUp(0);
            Vector3 inputPos = Input.mousePosition;
            
            if (Input.touchCount > 0 && !inputDown && !inputHeld && !inputUp)
            {
                Touch touch = Input.GetTouch(0);
                inputDown = touch.phase == TouchPhase.Began;
                inputHeld = touch.phase == TouchPhase.Stationary || touch.phase == TouchPhase.Moved;
                inputUp = touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled;
                inputPos = touch.position;
            }
            
            if (inputDown) // Touch begin
            {
                Vector3 worldPos = _cam.ScreenToWorldPoint(inputPos);
                GameObject topTile = GetTopTile(worldPos);
            
                if (topTile != null)
                {
                    _selectedTile = topTile;
                    _originalScale = _selectedTile.transform.localScale;
                    _pressTime = Time.time;
                    _isHolding = false;
                    _isPressed = true;
                    _isBlocked = _selectedTile.GetComponent<TileView>().Tile.IsBlocked;
                    
                    Most_HapticFeedback.Generate(Most_HapticFeedback.HapticTypes.Selection);
                }
            }
            
            if (_isPressed && inputHeld && _selectedTile != null && !_isBlocked) // Touch is Hold
            {
                if (!_isHolding && Time.time - _pressTime > 0.2f)
                {
                    _selectedTile.transform.localScale = _originalScale * 1.3f;
                    _selectedTile.GetComponent<SpriteRenderer>().sortingOrder += 10000;
                    _isHolding = true;
                    
                    Most_HapticFeedback.Generate(Most_HapticFeedback.HapticTypes.Selection);
                }
            }
            
            if (_isPressed && inputUp) // Touch end
            {
                if (_selectedTile != null)
                {
                    if (_isHolding)
                    {
                        _selectedTile.transform.localScale = _originalScale;
                        _selectedTile.GetComponent<SpriteRenderer>().sortingOrder -= 10000;
                    }
                    else
                    {
                        var tile = _selectedTile.GetComponent<TileView>().Tile;
                        if (fieldHandler.Field.DeactivateTile(tile))
                            buffer.AddTile(tile);
                    }
                }
            
                _selectedTile = null;
                _isPressed = false;
                _isHolding = false;
            }
        }
    
        GameObject GetTopTile(Vector3 worldPos)
        {
            Collider2D[] hits = Physics2D.OverlapPointAll(worldPos, tileLayer);
        
            if (hits.Length == 0) return null;
        
            GameObject topTile = null;
            int highestOrder = int.MinValue;
        
            foreach (Collider2D hit in hits)
            {
                SpriteRenderer sr = hit.GetComponent<SpriteRenderer>();
                if (sr != null && sr.sortingOrder > highestOrder)
                {
                    highestOrder = sr.sortingOrder;
                    topTile = hit.gameObject;
                }
            }
        
            return topTile;
        }
    }
}