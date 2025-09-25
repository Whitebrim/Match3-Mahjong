using System;
using UnityEngine;

namespace Game.Tiles
{
    public class FieldFactoryFromTemplate : FieldFactory
    {
        private readonly LevelTemplateSO _levelTemplate;

        public FieldFactoryFromTemplate(LevelTemplateSO levelTemplate)
        {
            _levelTemplate = levelTemplate;
        }

        public override Field Create()
        {
            var gridX = _levelTemplate.width + 2;
            var gridY = _levelTemplate.height + 2;
            var field = new Field(gridX, gridY, _levelTemplate.layers);

            for (var z = _levelTemplate.layers - 1; z >= 0; z--)
            {
                for (var y = 1; y < gridY - 1; y++)
                {
                    for (var x = 1; x < gridX - 1; x++)
                    {
                        if (!_levelTemplate.Field[x - 1, y - 1, z]) continue;
                        
                        var pos = new Vector3Int(x, y, z);
                        var tile = new Tile
                        {
                            type = TileType.Tiles_3,
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