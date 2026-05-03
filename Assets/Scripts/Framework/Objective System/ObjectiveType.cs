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
    [Tooltip("This is used to count the required objectives without putting a number manually. For example, if completion event is to collect certain amount of hidden boxes, you can set the creation event to the event that creates the hidden boxes and use this parameter to count how many hidden boxes are created in the level.")]
    public GameEvent creationCountEvent;
    [Tooltip("Indicates which game event will reduce the number of required objectives when triggered. This is the main event that will be listened to for this objective.")]
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