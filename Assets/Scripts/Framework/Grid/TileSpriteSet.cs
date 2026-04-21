using Sirenix.OdinInspector;
using UnityEngine;

namespace Game
{
    [CreateAssetMenu(fileName = "TileSpriteSet", menuName = "Game/Tile Sprite Set", order = 2)]
    public class TileSpriteSet : ScriptableObject
    {
        [FoldoutGroup("Tiles")] public Sprite topLeftCorner;
        [FoldoutGroup("Tiles")] public Sprite topRightCorner;
        [FoldoutGroup("Tiles")] public Sprite bottomLeftCorner;
        [FoldoutGroup("Tiles")] public Sprite bottomRightCorner;
        [FoldoutGroup("Tiles")] public Sprite topEdge;
        [FoldoutGroup("Tiles")] public Sprite bottomEdge;
        [FoldoutGroup("Tiles")] public Sprite leftEdge;
        [FoldoutGroup("Tiles")] public Sprite rightEdge;
        [FoldoutGroup("Tiles")] public Sprite topTip;
        [FoldoutGroup("Tiles")] public Sprite bottomTip;
        [FoldoutGroup("Tiles")] public Sprite leftTip;
        [FoldoutGroup("Tiles")] public Sprite rightTip;
        [FoldoutGroup("Tiles")] public Sprite verticalTile;
        [FoldoutGroup("Tiles")] public Sprite horizontalTile;
        [FoldoutGroup("Tiles")] public Sprite verticalWithLeftConnectionTile;
        [FoldoutGroup("Tiles")] public Sprite verticalWithRightConnectionTile;
        [FoldoutGroup("Tiles")] public Sprite horizontalWithUpConnectionTile;
        [FoldoutGroup("Tiles")] public Sprite horizontalWithDownConnectionTile;
        [FoldoutGroup("Tiles")] public Sprite onlyVerticalAndHorizontalConnectionTile;
        [FoldoutGroup("Tiles")] public Sprite upperAndLeftTile;
        [FoldoutGroup("Tiles")] public Sprite upperAndRightTile;
        [FoldoutGroup("Tiles")] public Sprite lowerAndLeftTile;
        [FoldoutGroup("Tiles")] public Sprite lowerAndRightTile;
        [FoldoutGroup("Tiles")] public Sprite singleTileStandalone;
        [FoldoutGroup("Tiles")] public Sprite topLeftConvexCorner;
        [FoldoutGroup("Tiles")] public Sprite topRightConvexCorner;
        [FoldoutGroup("Tiles")] public Sprite bottomLeftConvexCorner;
        [FoldoutGroup("Tiles")] public Sprite bottomRightConvexCorner;
        [FoldoutGroup("Tiles")] public Sprite singleInnerTile;
        [FoldoutGroup("Tiles")] public Sprite errorTile;
    }
}
