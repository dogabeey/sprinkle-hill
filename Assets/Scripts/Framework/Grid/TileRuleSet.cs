using UnityEngine;

namespace Game
{
    [System.Serializable]
    public abstract class TileRuleSet
    {
        public abstract Tiles GetTiles(Vector2Int coordinates);

        public virtual Tiles GetTiles(Vector2Int coordinates, Grid3D.GridCell cell)
        {
            return GetTiles(coordinates);
        }
    }

    [System.Serializable]
    public class SingleTileRuleSet : TileRuleSet
    {
        public Tiles tiles;

        public override Tiles GetTiles(Vector2Int coordinates) => tiles;
    }

    [System.Serializable]
    public class CheckeredTileRuleSet : TileRuleSet
    {
        public Tiles tiles1;
        public Tiles tiles2;

        public override Tiles GetTiles(Vector2Int coordinates)
        {
            bool isEven = (coordinates.x + coordinates.y) % 2 == 0;
            return isEven ? tiles1 : tiles2;
        }
    }
}
