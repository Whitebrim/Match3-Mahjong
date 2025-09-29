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

        [SerializeField] private float colorTint = 0.15f;
        [SerializeField] private float yMultiplier = 0.93f;
        [SerializeField] private float depthShift = 0.36f;

        public Tile Tile
        {
            get => tile;
            set
            {
                if (tile != null)
                {
                    tile.OnActiveChanged -= SetActive;
                    tile.OnBlockedChanged -= OnBlockedChanged;
                    tile.OnBlockedLayersChanged -= OnBlockedLayersChanged;
                    tile.OnTopmostTileInThisStackChanged -= OnTopmostTileInThisStackChanged;
                }
                
                tile = value;
                
                if (tile != null)
                {
                    tile.OnActiveChanged += SetActive;
                    tile.OnBlockedChanged += OnBlockedChanged;
                    tile.OnBlockedLayersChanged += OnBlockedLayersChanged;
                    tile.OnTopmostTileInThisStackChanged += OnTopmostTileInThisStackChanged;
                    UpdateView();
                }
            }
        }

        private void UpdateView()
        {
            if (tile == null) return;
            
            OnBlockedLayersChanged(tile.BlockedByLayers);
            SetActive(tile.Active);
            UpdatePosition(tile.gridPosition, tile.TopmostTileInThisStack - tile.gridPosition.z);
            SetSprite(tile.type);
            UpdateSortingOrder(tile.gridPosition);
        }
        
        private void UpdatePosition(Vector3Int pos, int depth) =>
            transform.SetLocalPositionAndRotation(new Vector3(pos.x, pos.y * yMultiplier - depth * depthShift, pos.z), Quaternion.identity);

        private async void SetSprite(TileType type) =>
            spriteRenderer.sprite = await _config.TileSprites[type].LoadAndCacheAsync(ReleaseKey.Game);

        private void UpdateSortingOrder(Vector3Int pos) =>
            spriteRenderer.sortingOrder = (tile!.IsObtainable ? 10000 : pos.z * 100) - pos.y;

        private void OnBlockedChanged(int blocks) => UpdateSortingOrder(tile!.gridPosition);
        
        private void OnBlockedLayersChanged(int blocks) => TintTile(blocks);

        private void TintTile(int tintLevel)
        {
            var brightness = 1 - tintLevel * colorTint;
            spriteRenderer.color = new Color(brightness, brightness,
                brightness);
        }

        private void SetActive(bool value) => gameObject.SetActive(value);

        private void OnTopmostTileInThisStackChanged(int newValue) => UpdatePosition(tile!.gridPosition, newValue - tile.gridPosition.z);
    }
}
