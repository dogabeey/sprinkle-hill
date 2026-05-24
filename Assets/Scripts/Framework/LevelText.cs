using Game;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine; using Game.EventManagement;

public class LevelText : MonoBehaviour
{
    public TMP_Text levelText;

    private void Update()
    {
        levelText.text = "LEVEL " + (GameManager.Instance.CurrentWorld.lastPlayedLevelIndex + 1).ToString();
    }
}
