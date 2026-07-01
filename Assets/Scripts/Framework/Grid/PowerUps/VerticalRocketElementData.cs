using Sirenix.OdinInspector;
using UnityEngine;
[CreateAssetMenu(fileName = "VerticalRocketElementData", menuName = "Game/Elements/Power-Ups/Vertical Rocket Element Data...")]
public class VerticalRocketElementData : PowerUpElementData
{
    [FoldoutGroup("Animation")]
    public Sprite upperPieceSprite;
    [FoldoutGroup("Animation")]
    public Sprite lowerPieceSprite;
}
