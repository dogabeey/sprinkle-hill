using Sirenix.OdinInspector;
using UnityEngine;

[CreateAssetMenu(fileName = "New Dialogue", menuName = "Game/New Dialogue...")]
public class Dialogue : ScriptableObject
{
    public string dialogueID;
    public DialogueNode rootNode;
    [AssetsOnly]
    public DialogueUI dialogueUIPrefab;
}
