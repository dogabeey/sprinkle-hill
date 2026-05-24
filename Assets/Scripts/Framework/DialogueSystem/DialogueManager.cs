using System;
using System.Collections.Generic;
using UnityEngine; using Game.EventManagement;

public class DialogueManager : MonoBehaviour
{
    public List<Dialogue> availableDialogues;

    public void StartDialogue(string dialogueID)
    {
        Dialogue dialogue = availableDialogues.Find(d => d.dialogueID == dialogueID);
        if (dialogue != null)
        {
            DialogueUI dialogueUI = Instantiate(dialogue.dialogueUIPrefab);
            dialogueUI.StartDialogue(dialogue);
        }
        else
        {
            Debug.LogWarning($"Dialogue with ID '{dialogueID}' not found.");
        }
    }
}