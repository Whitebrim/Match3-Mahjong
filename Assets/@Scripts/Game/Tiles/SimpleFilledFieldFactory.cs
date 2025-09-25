using System;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Game.Tiles
{
    public class SimpleFilledFieldFactory : FieldFactory
    {
        private readonly int _tilesX;
        private readonly int _tilesY;
        private readonly int _tilesZ;

        public SimpleFilledFieldFactory(int tilesX, int tilesY, int tilesZ)
        {
            _tilesX = tilesX;
            _tilesY = tilesY;
            _tilesZ = tilesZ;
        }

        public override Field Create()
        {
            var gridX = _tilesX * 2 - 1 + 2;
            var gridY = _tilesY * 2 - 1 + 2;
            var field = new Field(gridX, gridY, _tilesZ);

            for (var z = 0; z < _tilesZ; z++)
            {
                for (var y = 1 + z % 2; y < gridY - 1; y += 2)
                {
                    for (var x = 1 + z % 2; x < gridX - 1; x += 2)
                    {
                        var pos = new Vector3Int(x, y, z);
                        var tile = new Tile
                        {
                            type = (TileType)Random.Range(0, Enum.GetValues(typeof(TileType)).Length),
                            gridPosition = pos
                        };
                        
                        if (!field.PlaceTile(tile))
                            throw new Exception("Unexpectedly can't place tile at " + pos);
                    }
                }
            }

            return field;
        }
    }
}