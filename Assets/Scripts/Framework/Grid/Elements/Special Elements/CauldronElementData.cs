using Sirenix.OdinInspector;
using UnityEngine;

[CreateAssetMenu(fileName = "CauldronElementData", menuName = "Game/Elements/Special/Cauldron Element Data...")]
public class CauldronElementData : PowerUpElementData
{
    [FoldoutGroup("Cauldron"), ShowIf(nameof(IsCauldron))]
    [Min(1)] public int cauldronChargeRequired = 8;
    [FoldoutGroup("Cauldron"), ShowIf(nameof(IsCauldron))]
    [Min(1)] public int cauldronChargeRadius = 1;
    [FoldoutGroup("Cauldron"), ShowIf(nameof(IsCauldron))]
    public ParticleSystem cauldronExplosionParticle;
    [FoldoutGroup("Cauldron"), ShowIf(nameof(IsCauldron))]
    public ElementAnimationByProgress[] elementAnimationsByProgress;

    private bool IsCauldron() => true; // This can be modified later if we want to use this data for other purposes as well.
}

[CreateAssetMenu(fileName = "PowerGeneratorElementData", menuName = "Game/Elements/Special/Power Generator Element Data...")]
public class PowerGeneratorElementData : PowerUpElementData
{
}

[CreateAssetMenu(fileName = "PowerOutletElementData", menuName = "Game/Elements/Special/Power Outlet Element Data...")]
public class PowerOutletElementData : PowerUpElementData
{
}

[CreateAssetMenu(fileName = "GarbageBagElementData", menuName = "Game/Elements/Special/Garbage Bag Element Data...")]
public class GarbageBagElementData : PowerUpElementData
{
}