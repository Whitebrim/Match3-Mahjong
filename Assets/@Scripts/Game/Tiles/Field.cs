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

        /// <summary>
        /// !! Place tiles from top to bottom for block system to work.
        /// </summary>
        /// <param name="tile"></param>
        /// <returns></returns>
        public bool PlaceTile(Tile tile)
        {
            var pos = tile.gridPosition;
            if (!CanPlace(pos)) return false;
            Grid[pos.x, pos.y, pos.z] = tile;
            
            for (var dx = -1; dx <= 1; dx++)
            for (var dy = -1; dy <= 1; dy++)
            {
                AddOrCreateOccupiedTile(tile, new Vector3Int(pos.x + dx, pos.y + dy, pos.z));
            }

            UpdateBlockStatus(tile);
            UpdateTopmostFieldOnStack(pos.x, pos.y);
            
            return true;
        }

        private void AddOrCreateOccupiedTile(Tile origin, Vector3Int pos)
        {
            if (Grid[pos.x, pos.y, pos.z] is Tile) return;
            Grid[pos.x, pos.y, pos.z] ??= new OccupiedTile();
            (Grid[pos.x, pos.y, pos.z] as OccupiedTile)?.Origin.Add(origin);
        }
        
        private void UpdateBlockStatus(Tile tile)
        {
            var pos = tile.gridPosition;

            tile.BlockedBy = 0;
            
            int maxBlockedLayers = 0;
            for (var dx = -1; dx <= 1; dx++)
            for (var dy = -1; dy <= 1; dy++)
            {
                if (pos.z >= _sizeZ - 1) continue;
                if (Grid[pos.x + dx, pos.y + dy, pos.z + 1] is Tile { Active: true } tileAbove)
                {
                    maxBlockedLayers = Mathf.Max(maxBlockedLayers, tileAbove.BlockedByLayers);
                    tile.BlockedBy++;
                }
            }
            tile.BlockedByLayers = maxBlockedLayers + 1;
        }

        private void UpdateTopmostFieldOnStack(int x, int y)
        {
            int? topmost = null;
            for (var z = _sizeZ - 1; z >= 0; z--)
            {
                if (Grid[x, y, z] is not Tile stackTile) continue;
                topmost ??= z;
                stackTile.TopmostTileInThisStack = topmost.Value;
            }
        }
        
        public bool DeactivateTile(Tile tile)
        {
            if (tile is null || tile.IsBlocked) return false;
            var pos = tile.gridPosition;
            for (var dx = -1; dx <= 1; dx++)
            for (var dy = -1; dy <= 1; dy++)
            {
                if (Grid[pos.x + dx, pos.y + dy, pos.z] is OccupiedTile occupiedTile)
                {
                    occupiedTile.Origin.Remove(tile);
                    if (occupiedTile.Origin.Count == 0)
                        Grid[pos.x + dx, pos.y + dy, pos.z] = null;
                }
            }

            tile.Active = false;
            
            if (pos.z > 0)
            {
                for (var z = _sizeZ - 2; z >= 0; z--)
                for (var x = 1; x < _sizeX - 1; x++)
                for (var y = 1; y < _sizeY - 1; y++)
                {
                    if (Grid[x, y, z] is Tile selectedTile)
                        UpdateBlockStatus(selectedTile);
                }
            }

            int? topmost = null;
            for (var z = pos.z - 1; z >= 0; z--)
            {
                if (Grid[pos.x, pos.y, z] is not Tile tileBelow) continue;
                topmost ??= z;
                tileBelow.TopmostTileInThisStack = topmost.Value;
            }
            
            return true;
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