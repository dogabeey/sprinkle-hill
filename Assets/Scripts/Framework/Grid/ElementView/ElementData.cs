using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu(fileName = "ElementVisualData", menuName = "Game/Element Data...")]
public class ElementData : ScriptableObject
{
    public string elementName;
    [ColorUsage(false)]
    public Color ElementColor;
    public Mesh elementMesh;
    public Material elementMaterial;
    public Sprite ElementSprite;

    public static ElementData GetElementDataByName(string name, List<ElementData> elementDataList)
    {
        if (elementDataList == null)
        {
            return null;
        }
        for (int i = 0; i < elementDataList.Count; i++)
        {
            if (elementDataList[i] != null && elementDataList[i].elementName == name)
            {
                return elementDataList[i];
            }
        }
        return null;
    }
}