using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ElementVisualData", menuName = "Game/Element Visual Data")]
public class ElementVisualDataManager : SerializedScriptableObject
{
    public List<ElementData> elementVisuals = new List<ElementData>();
}

[CreateAssetMenu(fileName = "ElementVisualData", menuName = "Game/Element Data...")]
public class ElementData
{
    public string elementName;
    [ColorUsage(false)]
    public Color ElementColor;
    public Mesh elementMesh;
    public Material elementMaterial;
    public Sprite ElementSprite;
}