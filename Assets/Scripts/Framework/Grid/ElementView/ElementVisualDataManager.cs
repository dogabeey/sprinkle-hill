using Sirenix.OdinInspector;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ElementVisualData", menuName = "Game/Element Visual Data")]
public class ElementVisualDataManager : SerializedScriptableObject
{
    public Dictionary<ElementVisualType, ElementVisualInfo> elementVisuals;

    public ElementVisualInfo GetElementVisualInfo(ElementVisualType visualType)
    {
        if (elementVisuals.TryGetValue(visualType, out ElementVisualInfo visualInfo))
        {
            return visualInfo;
        }
        else
        {
            Debug.LogWarning($"ElementVisualData: No visual info found for type {visualType}");
            return null;
        }
    }
#if UNITY_EDITOR
    /// <summary>
    /// Generate material assets based on the colors defined in the ElementVisualInfo. Prompt a folder selection dialog to choose where to save the materials.
    /// </summary>
    [Button]
    public void GenerateMaterialsBasedOnColor(string shader = "Universal Render Pipeline/Lit")
    {
        PopulateDictionary(); // Populate the dictionary to ensure all ElementVisualTypes are included before generating materials.

        string folderPath = UnityEditor.EditorUtility.OpenFolderPanel("Select Folder to Save Materials", "Assets/", "");
        if (string.IsNullOrEmpty(folderPath))
        {
            Debug.LogWarning("Material generation canceled: No folder selected.");
            return;
        }
        // Convert absolute path to relative path
        if (folderPath.StartsWith(Application.dataPath))
        {
            folderPath = "Assets" + folderPath.Substring(Application.dataPath.Length);
        }
        foreach (var kvp in elementVisuals)
        {
            ElementVisualType visualType = kvp.Key;
            ElementVisualInfo visualInfo = kvp.Value;
            Material newMaterial = new Material(Shader.Find(shader));

            newMaterial.color = visualInfo.ElementColor;
            string materialPath = $"{folderPath}/{visualType}_Material.mat";
            UnityEditor.AssetDatabase.CreateAsset(newMaterial, materialPath);
            Debug.Log($"Generated material for {visualType} at {materialPath}");

            // Asign the newly created material back to the ElementVisualInfo
            visualInfo.elementMaterial = newMaterial;
        }
        UnityEditor.AssetDatabase.SaveAssets();
        UnityEditor.AssetDatabase.Refresh();
    }
    /// <summary>
    /// Create a button in the inspector to populate the elementVisuals dictionary with unset values for each ElementVisualType.
    /// </summary>
    private void PopulateDictionary()
    {
        foreach (ElementVisualType visualType in System.Enum.GetValues(typeof(ElementVisualType)))
        {
            if (!elementVisuals.ContainsKey(visualType))
            {
                elementVisuals.Add(visualType, new ElementVisualInfo());
                Debug.Log($"Added {visualType} to elementVisuals dictionary.");
            }
        }
    }
#endif
}

[System.Serializable]
public class ElementVisualInfo
{
    [ColorUsage(false)]
    public Color ElementColor;
    public Mesh elementMesh;
    public Material elementMaterial;
    public Sprite ElementSprite;
}

public enum ElementVisualType
{
    White,
    Blue,
    Green,
    Yellow,
    Red,
    Purple,
    Orange,
    Cyan,
    Pink,
    DarkGreen,
    DarkBlue,
}