using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class VisualizableScriptableObject : ScriptableObject
{
    public string displayName;
    [ColorUsage(false)]
    public Color displayColor;
    public Sprite displayIcon;
}

[CreateAssetMenu(fileName = "ElementVisualData", menuName = "Game/Element Data...")]
public class ElementData : VisualizableScriptableObject
{
    [System.Flags]
    public enum ElementBehaviorFlags
    {
        None = 0,
        NonSwappable = 1 << 0,
        NonMatchable = 1 << 1,
        NonShuffleable = 1 << 2,
        PassThrough = 1 << 3,
        ImmuneToClear = 1 << 4
    }

    [FoldoutGroup("General")]
    public Sprite breakableWallOverride;
    public Mesh elementMesh;
    public Material elementMaterial;
    public Vector2Int gridCoverage = Vector2Int.one;
    [FoldoutGroup("General")]
    public ElementBehaviorFlags behaviorFlags;
    [FoldoutGroup("Animation")]
    public string defaultIdleAnimation = "idle";
    [FoldoutGroup("Animation")]
    public RuntimeAnimatorController animationController;
    [FoldoutGroup("Cauldron"), ShowIf(nameof(IsCauldron))]
    [Min(1)] public int cauldronChargeRequired = 8;
    [FoldoutGroup("Cauldron"), ShowIf(nameof(IsCauldron))]
    [Min(1)] public int cauldronChargeRadius = 1;
    [FoldoutGroup("Cauldron"), ShowIf(nameof(IsCauldron))]
    public ParticleSystem cauldronExplosionParticle;
    [FoldoutGroup("Cauldron"), ShowIf(nameof(IsCauldron))]
    public ElementAnimationByProgress[] elementAnimationsByProgress;

    [Serializable]
    public class ElementAnimationByProgress
    {
        [MinMaxSlider(0, 1)] public Vector2 progressRange;
        public int animationLayer;
    }

    public ElementBehaviorFlags GetEffectiveBehaviorFlags()
    {
        ElementBehaviorFlags flags = behaviorFlags;

        Game.GameManager manager = Game.GameManager.Instance;
        if (manager != null)
        {
            if (manager.garbageBagElementData == this)
            {
                flags |= ElementBehaviorFlags.NonSwappable |
                         ElementBehaviorFlags.NonMatchable |
                         ElementBehaviorFlags.NonShuffleable |
                         ElementBehaviorFlags.ImmuneToClear;
            }

            if (manager.powerGeneratorElementData == this)
            {
                flags |= ElementBehaviorFlags.NonSwappable |
                         ElementBehaviorFlags.NonMatchable |
                         ElementBehaviorFlags.NonShuffleable |
                         ElementBehaviorFlags.PassThrough |
                         ElementBehaviorFlags.ImmuneToClear;
            }

            if (manager.powerOutletElementData == this)
            {
                flags |= ElementBehaviorFlags.NonSwappable |
                         ElementBehaviorFlags.NonMatchable |
                         ElementBehaviorFlags.NonShuffleable |
                         ElementBehaviorFlags.PassThrough |
                         ElementBehaviorFlags.ImmuneToClear;
            }
        }

        return flags;
    }

    public bool HasBehavior(ElementBehaviorFlags flag)
    {
        return (GetEffectiveBehaviorFlags() & flag) != 0;
    }

    public static ElementData GetElementDataByName(string name, List<ElementData> elementDataList)
    {
        if (elementDataList == null)
        {
            return null;
        }
        for (int i = 0; i < elementDataList.Count; i++)
        {
            if (elementDataList[i] != null && elementDataList[i].displayName == name)
            {
                return elementDataList[i];
            }
        }
        return null;
    }

#if UNITY_EDITOR
    private bool IsCauldron
    {
        get
        {
            Game.GameManager manager = Game.GameManager.Instance;
            if (manager == null)
                manager = UnityEngine.Object.FindAnyObjectByType<Game.GameManager>();

            return manager != null && manager.cauldronElementData == this;
        }
    }
#else
    private bool IsCauldron => false;
#endif

}