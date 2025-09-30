using System.Collections.Generic;
using System.Linq;
using Game.Tiles;
using Solo.MOST_IN_ONE;
using UnityEngine;

namespace Game
{
    public class PowerUps : MonoBehaviour
    {
        [SerializeField] private Buffer buffer;
        [SerializeField] private FieldHandler fieldHandler;

        public void UndoClick()
        {
            var tile = buffer.Undo();
            if (tile is null) return;
            fieldHandler.Field.ReactivateTile(tile);
        }

        public void ShuffleClick() => fieldHandler?.Field?.Shuffle();

        public void WandClick()
        {
            if (fieldHandler?.Field == null || buffer == null) return;
            TileType targetType;
            var existingInBuffer = 0;
            if (buffer.tiles.Count > 0)
            {
                var typeCounts = new Dictionary<TileType, int>();
                foreach (var tile in buffer.tiles)
                {
                    typeCounts.TryAdd(tile.type, 0);
                    typeCounts[tile.type]++;
                }

                var maxType = typeCounts.OrderByDescending(x => x.Value).First();
                targetType = maxType.Key;
                existingInBuffer = maxType.Value;
            }
            else
            {
                var obtainableTile = FindAnyObtainableTile();
                if (obtainableTile == null) return;
                targetType = obtainableTile.type;
            }

            var needToCollect = 3 - existingInBuffer;
            CollectTilesFromField(targetType, needToCollect);
            buffer.tiles.RemoveAll(t => t.type == targetType);
            buffer.Wand();
            buffer.UpdateBufferUI();
            Most_HapticFeedback.Generate(Most_HapticFeedback.HapticTypes.Success);
        }

        private Tile FindAnyObtainableTile()
        {
            for (var z = fieldHandler.Field.Grid.GetLength(2) - 1; z >= 0; z--)
            {
                for (var y = 0; y < fieldHandler.Field.Grid.GetLength(1); y++)
                {
                    for (var x = 0; x < fieldHandler.Field.Grid.GetLength(0); x++)
                    {
                        if (fieldHandler.Field.Grid[x, y, z] is Tile tile && tile.Active && tile.IsObtainable)
                        {
                            return tile;
                        }
                    }
                }
            }

            return null;
        }

        private void CollectTilesFromField(TileType targetType, int count)
        {
            var tilesToDeactivate = new List<Tile>();
            for (var z = fieldHandler.Field.Grid.GetLength(2) - 1; z >= 0; z--)
            {
                for (var y = 0; y < fieldHandler.Field.Grid.GetLength(1); y++)
                {
                    for (var x = 0; x < fieldHandler.Field.Grid.GetLength(0); x++)
                    {
                        if (fieldHandler.Field.Grid[x, y, z] is Tile tile && tile.Active && tile.type == targetType &&
                            tile.IsObtainable)
                        {
                            tilesToDeactivate.Add(tile);
                        }
                    }
                }
            }

            if (tilesToDeactivate.Count < count)
            {
                for (var z = fieldHandler.Field.Grid.GetLength(2) - 1; z >= 0 && tilesToDeactivate.Count < count; z--)
                {
                    for (var y = 0; y < fieldHandler.Field.Grid.GetLength(1) && tilesToDeactivate.Count < count; y++)
                    {
                        for (var x = 0;
                             x < fieldHandler.Field.Grid.GetLength(0) && tilesToDeactivate.Count < count;
                             x++)
                        {
                            if (fieldHandler.Field.Grid[x, y, z] is Tile tile && tile.Active &&
                                tile.type == targetType && !tilesToDeactivate.Contains(tile))
                            {
                                tilesToDeactivate.Add(tile);
                            }
                        }
                    }
                }
            }

            for (var i = 0; i < Mathf.Min(count, tilesToDeactivate.Count); i++)
            {
                fieldHandler.Field.DeactivateTile(tilesToDeactivate[i], forced: true);
            }
        }
    }
}