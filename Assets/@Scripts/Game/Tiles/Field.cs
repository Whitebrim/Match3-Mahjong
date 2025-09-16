using UnityEngine;

namespace Game.Tiles
{
    public class Field
    {
        private readonly BaseTile[,,] _grid;
        private readonly int _sizeX, _sizeY, _sizeZ;

        public Field(int sizeX, int sizeY, int sizeZ) {
            _sizeX = sizeX;
            _sizeY = sizeY;
            _sizeZ = sizeZ;
            _grid = new BaseTile[sizeX, sizeY, sizeZ];
        }

        private bool IsInsidePlayableArea(Vector3Int pos) {
            return pos.x > 0 && pos.x < _sizeX - 1 &&
                   pos.y > 0 && pos.y < _sizeY - 1 &&
                   pos.z >= 0 && pos.z < _sizeZ; // height layer
        }

        private bool CanPlace(Vector3Int pos)
        {
            if (!IsInsidePlayableArea(pos)) return false;
            for (var dx = -1; dx <= 1; dx++)
            for (var dy = -1; dy <= 1; dy++)
                if (_grid[pos.x + dx, pos.y + dy, pos.z] != null) return false;
            return true;
        }

        public bool PlaceTile(Tile tile)
        {
            var pos = tile.GridPosition;
            if (!CanPlace(pos)) return false;
            _grid[pos.x, pos.y, pos.z] = tile;
            for (var dx = -1; dx <= 1; dx++)
            for (var dy = -1; dy <= 1; dy++)
                AddOrCreateOccupiedTile(tile, new Vector3Int(pos.x + dx, pos.y + dy, pos.z));
            return true;
        }

        private void AddOrCreateOccupiedTile(Tile tile, Vector3Int pos)
        {
            if (_grid[pos.x, pos.y, pos.z] is Tile) return;
            _grid[pos.x, pos.y, pos.z] ??= new OccupiedTile();
            (_grid[pos.x, pos.y, pos.z] as OccupiedTile)?.Origin.Add(tile);
        }

        public void RemoveTile(Vector3Int pos) {
            // Todo add checks if needed
            for (var dx = -1; dx <= 1; dx++)
            for (var dy = -1; dy <= 1; dy++)
                _grid[pos.x + dx, pos.y + dy, pos.z] = null;
                    
            if (pos.z > 0) // Remove blocks
            {
                // Todo add block remove
            }
        }
    }
}