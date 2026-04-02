using Game;
using Sirenix.OdinInspector;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public partial class DialogueUI : UIElement
{

    public DialogueNode currentNode;
    public TMP_Text characterNameText;
    public Image avatarImage;
    public TMP_Text dialogueText;
    public Transform choicesContainer;
    [AssetsOnly]
    public DialogueChoiceUI choicePrefab;


    public override void InitUI()
    {
    }

    public override void DrawUI()
    {
        if(currentNode != null)
        {
                characterNameText.text = currentNode.characterName;
                avatarImage.sprite = currentNode.avatar;
                dialogueText.text = currentNode.dialogueText;
    
                // Clear existing choices
                foreach (Transform child in transform)
                {
                    Destroy(child.gameObject);
                }
    
                // Create choice buttons
                foreach (var choice in currentNode.choices)
                {
                    var choiceUI = Instantiate(choicePrefab, choicesContainer);
                    choiceUI.Init(this, choice);
            }
        }
    }

    internal void StartDialogue(Dialogue dialogue)
    {
        currentNode = dialogue.rootNode;
        DrawUI();
    }
}
