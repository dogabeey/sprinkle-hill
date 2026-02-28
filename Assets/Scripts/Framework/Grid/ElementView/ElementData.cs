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
}