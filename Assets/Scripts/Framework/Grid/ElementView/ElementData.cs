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
    public Mesh elementMesh;
    public Material elementMaterial;
    public Vector2Int gridCoverage = Vector2Int.one;
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
            string[] levelGuids = AssetDatabase.FindAssets("t:LevelScene_Match3Game");
            for (int i = 0; i < levelGuids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(levelGuids[i]);
                Game.LevelScene_Match3Game levelScene = AssetDatabase.LoadAssetAtPath<Game.LevelScene_Match3Game>(path);
                if (levelScene != null && levelScene.cauldronElementData == this)
                    return true;
            }

            return false;
        }
    }
#else
    private bool IsCauldron => false;
#endif

}