using System;
using UnityEngine;

namespace Game.Tiles
{
    [Serializable]
    public class Tile : BaseTile
    {
        public int id;
        public TileType type;
        public Vector3Int gridPosition;
        public bool isBlocked;
        public GameObject gameObject;
    }
}