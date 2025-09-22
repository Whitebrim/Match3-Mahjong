using Core.Services.AssetManagement;
using JetBrains.Annotations;
using UnityEngine;
using Utils.Extensions;
using VContainer;

namespace Game.Tiles
{
    public class TileView : MonoBehaviour
    {
        [Inject] private TileDictionaryConfig _config;
        
        [SerializeField, CanBeNull] private Tile tile;
        [SerializeField] private SpriteRenderer spriteRenderer;

        private readonly Color _normalColor = Color.white;
        private readonly Color _blockedColor = new(0.6f, 0.6f, 0.6f);

        public Tile Tile
        {
            get => tile;
            set
            {
                if (tile != null)
                {
                    tile.OnActiveChanged -= SetActive;
                    tile.OnBlockedChanged -= OnBlockedChanged;
                }
                
                tile = value;
                
                if (tile != null)
                {
                    tile.OnActiveChanged += SetActive;
                    tile.OnBlockedChanged += OnBlockedChanged;
                    UpdateView();
                }
            }
        }

        private void UpdateView()
        {
            if (tile == null) return;
            
            OnBlockedChanged(tile.BlockedBy);
            SetActive(tile.Active);
            UpdatePosition(tile.gridPosition);
            SetSprite(tile.type);
            SetSortingOrder(tile.gridPosition);
        }
        
        private void UpdatePosition(Vector3Int pos) =>
            transform.SetLocalPositionAndRotation(new Vector3(pos.x, pos.y + pos.z * 0.25f, pos.z), Quaternion.identity);

        private async void SetSprite(TileType type) =>
            spriteRenderer.sprite = await _config.TileSprites[type].LoadAndCacheAsync(ReleaseKey.Game);

        private void SetSortingOrder(Vector3Int pos) => spriteRenderer.sortingOrder = pos.z * 100 - pos.y;

        private void OnBlockedChanged(int blocks) => TintTile(blocks > 0);

        private void TintTile(bool tint) => spriteRenderer.color = tint ? _blockedColor : _normalColor;

        private void SetActive(bool value) => gameObject.SetActive(value);
    }
}
