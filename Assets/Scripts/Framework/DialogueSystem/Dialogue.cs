using Sirenix.OdinInspector;
using UnityEngine;

[CreateAssetMenu(fileName = "New Dialogue", menuName = "Lionsfall/New Dialogue...")]
public class Dialogue : ScriptableObject
{
    public string dialogueID;
    public DialogueNode rootNode;
    [AssetsOnly]
    public DialogueUI dialogueUIPrefab;
}
