using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using UnityEngine; using Game.EventManagement;

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

public class PowerUpElementData : ElementData
{
    [FoldoutGroup("Animation")]
    public string powerUpActivationString;
}

[CreateAssetMenu(fileName = "ElementVisualData", menuName = "Game/Elements/Element Data...")]
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

}