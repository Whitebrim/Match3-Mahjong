using System;
using UnityEngine;

namespace Game.Tiles
{
    [Serializable]
    public class Tile : BaseTile
    {
        private bool _active = true;
        private int _blockedBy = 0;
        
        public int id;
        public TileType type;
        public Vector3Int gridPosition;
        private TileView _view;
        
        public event Action<bool> OnActiveChanged;
        public event Action<int> OnBlockedChanged;
        
        public TileView View
        {
            get => _view;
            set
            {
                _view = value;
                value.Tile = this;
            }
        }

        public bool Active
        {
            get => _active;
            set
            {
                if (_active == value) return;
                _active = value;
                OnActiveChanged?.Invoke(_active);
            }
        }

        public int BlockedBy
        {
            get => _blockedBy;
            set
            {
                if (_blockedBy == value) return;
                _blockedBy = value;
                OnBlockedChanged?.Invoke(_blockedBy);
            }
        }
    }
}