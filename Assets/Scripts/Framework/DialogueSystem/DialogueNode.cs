using Game;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class DialogueNode
{
    public Sprite avatar;
    public string characterName;
    [TextArea(3, 10)]
    public string dialogueText;
    public DialogueChoice[] choices;
}

[System.Serializable]
public class DialogueChoice
{
    public string choiceText;
    public DialogueNode nextNode;
}
