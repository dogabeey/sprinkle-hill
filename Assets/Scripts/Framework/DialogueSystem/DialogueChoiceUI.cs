using TMPro;
using UnityEngine; using Game.EventManagement;
using UnityEngine.UI;

public class DialogueChoiceUI : MonoBehaviour
{
    internal DialogueUI referenceDialogueUI;
    internal DialogueChoice referenceChoice;

    public TMP_Text choiceText;
    public Button choiceButton;

    public void Init(DialogueUI dialogueUI, DialogueChoice choice)
    {
        this.referenceDialogueUI = dialogueUI;
        this.referenceChoice = choice;
        choiceText.text = choice.choiceText;
        choiceButton.onClick.AddListener(OnChoiceSelected);
    }

    private void OnChoiceSelected()
    {
        if (referenceChoice.nextNode != null)
        {
            referenceDialogueUI.currentNode = referenceChoice.nextNode;
            referenceDialogueUI.DrawUI();
        }
    }
}