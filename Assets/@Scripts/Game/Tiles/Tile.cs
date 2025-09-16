using UnityEngine;

namespace Game.Tiles
{
    public class Tile : BaseTile
    {
        public int ID;
        public TileType Type;
        public Vector3Int GridPosition;
        public bool IsBlocked;
        public GameObject GameObject;
    }
}