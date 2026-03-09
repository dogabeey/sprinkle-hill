using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using UnityEngine;

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