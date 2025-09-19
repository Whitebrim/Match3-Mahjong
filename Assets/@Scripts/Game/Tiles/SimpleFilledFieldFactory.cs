using System;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Game.Tiles
{
    public class SimpleFilledFieldFactory : FieldFactory
    {
        private static int _nextId = 0;

        public override Field Create(int sizeX, int sizeY, int sizeZ)
        {
            var gridX = sizeX * 2 + 1;
            var gridY = sizeY * 2 + 1;
            var field = new Field(gridX, gridY, sizeZ);

            for (var z = 0; z < sizeZ; z++)
            {
                for (var y = 1 + z % 2; y < gridY - 1; y += 2)
                {
                    for (var x = 1 + z % 2; x < gridX - 1; x += 2)
                    {
                        var pos = new Vector3Int(x, y, z);
                        var tile = new Tile
                        {
                            id = _nextId++,
                            type = (TileType)Random.Range(0, System.Enum.GetValues(typeof(TileType)).Length),
                            gridPosition = pos,
                            isBlocked = z < sizeZ - 1,
                            gameObject = null
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