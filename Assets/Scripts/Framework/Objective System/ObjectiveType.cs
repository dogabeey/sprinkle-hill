using UnityEngine;

[CreateAssetMenu(fileName = "ObjectiveType", menuName = "Game/Objective Type...")]
public class ObjectiveType : ScriptableObject
{
    [System.Flags]
    public enum CheckedParameterType
    {
        None = 0,
        GameObject = 1 << 0,
        String = 1 << 1,
        Int = 1 << 2,
        Float = 1 << 3,
        Bool = 1 << 4,
        Type = 1 << 5,
        ScriptableObject = 1 << 6
    }

    public string objectiveTypeName;
    [Tooltip("Use %o for the objective's display name and %c for the count of the objective.")]
    [TextArea]
    public string description;
    public GameEvent completionEvent;
    public Sprite objectiveTypeSprite; // This is fallback sprite used when the objective's scriptable object parameter doesn't exist or doesn't have a sprite.

    public string FormattedDescription(VisualizableScriptableObject visualizableScriptableObject, int count)
    {
        string displayName = visualizableScriptableObject != null && !string.IsNullOrEmpty(visualizableScriptableObject.displayName)
            ? visualizableScriptableObject.displayName
            : objectiveTypeName;

        return description.Replace("%o", displayName)
                          .Replace("%c", count.ToString());
    }

    public Sprite ResolveObjectiveSprite(VisualizableScriptableObject visualizableScriptableObject)
    {
        if (visualizableScriptableObject != null && visualizableScriptableObject.displayIcon != null)
            return visualizableScriptableObject.displayIcon;

        return objectiveTypeSprite;
    }
}