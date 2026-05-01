using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Rendering;

namespace Game
{
    [CreateAssetMenu(fileName = "TileSpriteSet", menuName = "Game/Tile Sprite Set", order = 2)]
    public class TileSpriteSet : ScriptableObject
    {
        [SerializeReference] public ColorRuleSet colorRuleset; // NOT IMPLEMENTED YET, but this is where you would specify rules for how to color the tiles in this set. This is separate from the sprites because some sets might want to use the same coloring rules.
        public Sprite topLeftCorner;
        public Sprite topRightCorner;
        public Sprite bottomLeftCorner;
        public Sprite bottomRightCorner;
        public Sprite topEdge;
        public Sprite bottomEdge;
        public Sprite leftEdge;
        public Sprite rightEdge;
        public Sprite topTip;
        public Sprite bottomTip;
        public Sprite leftTip;
        public Sprite rightTip;
        public Sprite verticalTile;
        public Sprite horizontalTile;
        public Sprite verticalWithLeftConnectionTile;
        public Sprite verticalWithRightConnectionTile;
        public Sprite horizontalWithUpConnectionTile;
        public Sprite horizontalWithDownConnectionTile;
        public Sprite onlyVerticalAndHorizontalConnectionTile;
        public Sprite upperAndLeftTile;
        public Sprite upperAndRightTile;
        public Sprite lowerAndLeftTile;
        public Sprite lowerAndRightTile;
        public Sprite singleTileStandalone;
        public Sprite topLeftConvexCorner;
        public Sprite topRightConvexCorner;
        public Sprite bottomLeftConvexCorner;
        public Sprite bottomRightConvexCorner;
        public Sprite singleInnerTile;
        public Sprite errorTile;
    }


    public abstract class ColorRuleSet
    {
        public BlendMode blendMode;

        public abstract Color GetTileColor(Vector2Int coordinates);
    }
    public class CheckerboardColorRuleSet : ColorRuleSet
    {
        public Color color1;
        public Color color2;
        public override Color GetTileColor(Vector2Int coordinates)
        {
            bool isEven = (coordinates.x + coordinates.y) % 2 == 0;
            return isEven ? color1 : color2;
        }
    }
}