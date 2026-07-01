using UnityEngine;
using Sirenix.OdinInspector;

[CreateAssetMenu(fileName = "HorizontalRocketElementData", menuName = "Game/Elements/Power-Ups/Horizontal Rocket Element Data...")]
public class HorizontalRocketElementData : RocketElementData
{
    [FoldoutGroup("Animation")]
    public Sprite leftPieceSprite;
    [FoldoutGroup("Animation")]
    public Sprite rightPieceSprite;
}
