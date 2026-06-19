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
        ImmuneToClear = 1 << 4,
        NotTargetableByPropeller = 1 << 5,
        NotAffectedByGravity = 1 << 6
    }

    public Sprite breakableWallOverride;
    public Mesh elementMesh;
    public Material elementMaterial;
    public Vector2Int gridCoverage = Vector2Int.one;
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


    public bool HasBehavior(ElementBehaviorFlags flag)
    {
        return (behaviorFlags & flag) != 0;
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