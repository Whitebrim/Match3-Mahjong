using System;
using UnityEngine;

namespace Game.Tiles
{
    [Serializable]
    public class Tile : BaseTile
    {
        private bool _active = true;
        private int _blockedBy;
        private int _blockedByLayers;
        private int _topmostTileInThisStack;
        
        public TileType type;
        public Vector3Int gridPosition;
        private TileView _view;
        
        public event Action<bool> OnActiveChanged;
        public event Action<int> OnBlockedChanged;
        public event Action<int> OnBlockedLayersChanged;
        public event Action<int> OnTopmostTileInThisStackChanged;
        
        public TileView View
        {
            get => _view;
            set
            {
                _view = value;
                value.Tile = this;
            }
        }

        /// <summary>
        /// Если игрок переместил этот тайл в буфер, то он деактивируется
        /// </summary>
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

        /// <summary>
        /// Доступен ли тайл для сбора
        /// </summary>
        public bool IsObtainable => BlockedBy <= 0;
        
        /// <summary>
        /// Заблокирован ли тайл для сбора
        /// </summary>
        public bool IsBlocked => BlockedBy > 0;
        
        /// <summary>
        /// Сколько тайлов на слою выше прямо блочат этот тайл
        /// </summary>
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
        
        /// <summary>
        /// Сколько слоев выше блочат этот тайл (для шейда тени)
        /// </summary>
        public int BlockedByLayers
        {
            get => _blockedByLayers;
            set
            {
                if (_blockedByLayers == value) return;
                _blockedByLayers = value;
                OnBlockedLayersChanged?.Invoke(_blockedByLayers);
            }
        }

        /// <summary>
        /// Какой Z у самого верхнего тайла в этой стопке (для ровного отображения стопок)
        /// </summary>
        public int TopmostTileInThisStack
        {
            get => _topmostTileInThisStack;
            set
            {
                if (_topmostTileInThisStack == value) return;
                _topmostTileInThisStack = value;
                OnTopmostTileInThisStackChanged?.Invoke(_topmostTileInThisStack);
            }
        }
    }
}