using Sirenix.OdinInspector;
using UnityEngine;
[CreateAssetMenu(fileName = "BombElementData", menuName = "Game/Elements/Power-Ups/Bomb Element Data...")]
public class BombElementData : PowerUpElementData
{
    [FoldoutGroup("Bomb")]
    public int bombClearRadius = 1;
    [FoldoutGroup("Bomb")]
    public int bombComboClearRadius = 10;
}
