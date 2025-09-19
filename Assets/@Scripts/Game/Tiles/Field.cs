using Sirenix.OdinInspector;
using UnityEngine;

namespace Game.Tiles
{
    public class Field
    {
        public readonly BaseTile[,,] Grid;
        private readonly int _sizeX, _sizeY, _sizeZ;

        public Field(int sizeX, int sizeY, int sizeZ) {
            _sizeX = sizeX;
            _sizeY = sizeY;
            _sizeZ = sizeZ;
            Grid = new BaseTile[sizeX, sizeY, sizeZ];
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
                if (Grid[pos.x + dx, pos.y + dy, pos.z] is Tile) return false;
            return true;
        }

        public bool PlaceTile(Tile tile)
        {
            var pos = tile.gridPosition;
            if (!CanPlace(pos)) return false;
            Grid[pos.x, pos.y, pos.z] = tile;
            for (var dx = -1; dx <= 1; dx++)
            for (var dy = -1; dy <= 1; dy++)
                AddOrCreateOccupiedTile(tile, new Vector3Int(pos.x + dx, pos.y + dy, pos.z));
            return true;
        }

        private void AddOrCreateOccupiedTile(Tile tile, Vector3Int pos)
        {
            if (Grid[pos.x, pos.y, pos.z] is Tile) return;
            Grid[pos.x, pos.y, pos.z] ??= new OccupiedTile();
            (Grid[pos.x, pos.y, pos.z] as OccupiedTile)?.Origin.Add(tile);
        }

        public void RemoveTile(Vector3Int pos) {
            // Todo add checks if needed
            for (var dx = -1; dx <= 1; dx++)
            for (var dy = -1; dy <= 1; dy++)
                Grid[pos.x + dx, pos.y + dy, pos.z] = null;
                    
            if (pos.z > 0) // Remove blocks
            {
                // Todo add block remove
            }
        }
        
        [Button("Debug Grid to Console", ButtonSizes.Medium)]
        private void DebugGridToConsole()
        {
            if (Grid == null)
            {
                Debug.Log("Grid is null");
                return;
            }

            Debug.Log($"=== GRID DEBUG INFO ===");
            Debug.Log($"Grid Size: {_sizeX} x {_sizeY} x {_sizeZ}");
            Debug.Log($"=======================");

            for (int z = 0; z < _sizeZ; z++)
            {
                Debug.Log($"\n--- LAYER {z} ---");
                
                for (int y = _sizeY - 1; y >= 0; y--)
                {
                    string row = $"Y{y:D2}: ";
                    
                    for (int x = 0; x < _sizeX; x++)
                    {
                        var tile = Grid[x, y, z];
                        string cellInfo;
                        
                        if (tile == null)
                        {
                            cellInfo = "NULL";
                        }
                        else
                        {
                            cellInfo = tile.GetType().Name;
                        }
                        
                        row += $"[{cellInfo,-12}] ";
                    }
                    
                    Debug.Log(row);
                }
                
                string xCoords = "   X: ";
                for (int x = 0; x < _sizeX; x++)
                {
                    xCoords += $" {x:D2}           ";
                }
                Debug.Log(xCoords);
            }
            
            Debug.Log("\n=== END GRID DEBUG ===");
        }
    }
}