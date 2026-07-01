using UnityEngine;
using Sirenix.OdinInspector;
using UnityEngine;

[CreateAssetMenu(fileName = "HorizontalRocketElementData", menuName = "Game/Elements/Power-Ups/Horizontal Rocket Element Data...")]
public class HorizontalRocketElementData : PowerUpElementData
{
    [FoldoutGroup("Animation")]
    public Sprite leftPieceSprite;
    [FoldoutGroup("Animation")]
    public Sprite rightPieceSprite;
}
