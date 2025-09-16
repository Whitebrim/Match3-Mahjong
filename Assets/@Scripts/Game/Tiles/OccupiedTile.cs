using System.Collections.Generic;

namespace Game.Tiles
{
    public class OccupiedTile : BaseTile
    {
        public readonly List<Tile> Origin = new(1);
    }
}