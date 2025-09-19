using JetBrains.Annotations;
using UnityEngine;

namespace Game.Tiles
{
    public class TileView : MonoBehaviour
    {
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
                    OnBlockedChanged(tile.BlockedBy);
                    SetActive(tile.Active);
                }
            }
        }

        private void OnBlockedChanged(int blocks) => TintTile(blocks > 0);

        private void TintTile(bool tint) => spriteRenderer.color = tint ? _blockedColor : _normalColor;

        private void SetActive(bool value) => gameObject.SetActive(value);
    }
}
