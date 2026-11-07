using Sirenix.OdinInspector;
using UnityEngine;

namespace Game
{
    [CreateAssetMenu(fileName = "Tiles", menuName = "Game/Tiles", order = 3)]
    public class Tiles : ScriptableObject
    {
        [PreviewField] public Sprite topLeftCorner;
        [PreviewField] public Sprite topRightCorner;
        [PreviewField] public Sprite bottomLeftCorner;
        [PreviewField] public Sprite bottomRightCorner;
        [PreviewField] public Sprite topEdge;
        [PreviewField] public Sprite bottomEdge;
        [PreviewField] public Sprite leftEdge;
        [PreviewField] public Sprite rightEdge;
        [PreviewField] public Sprite topTip;
        [PreviewField] public Sprite bottomTip;
        [PreviewField] public Sprite leftTip;
        [PreviewField] public Sprite rightTip;
        [PreviewField] public Sprite verticalTile;
        [PreviewField] public Sprite horizontalTile;
        [PreviewField] public Sprite verticalWithLeftConnectionTile;
        [PreviewField] public Sprite verticalWithRightConnectionTile;
        [PreviewField] public Sprite horizontalWithUpConnectionTile;
        [PreviewField] public Sprite horizontalWithDownConnectionTile;
        [PreviewField] public Sprite onlyVerticalAndHorizontalConnectionTile;
        [PreviewField] public Sprite upperAndLeftTile;
        [PreviewField] public Sprite upperAndRightTile;
        [PreviewField] public Sprite lowerAndLeftTile;
        [PreviewField] public Sprite lowerAndRightTile;
        [PreviewField] public Sprite singleTileStandalone;
        [PreviewField] public Sprite topLeftConvexCorner;
        [PreviewField] public Sprite topRightConvexCorner;
        [PreviewField] public Sprite bottomLeftConvexCorner;
        [PreviewField] public Sprite bottomRightConvexCorner;
        [PreviewField] public Sprite singleInnerTile;
        [PreviewField] public Sprite errorTile;
    }
}
