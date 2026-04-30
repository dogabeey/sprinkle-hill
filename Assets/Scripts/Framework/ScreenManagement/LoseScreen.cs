using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Game
{
    public class LoseScreen : GameScreen
    {
        public override Screens ScreenID => Screens.LoseScreen;

        public TMP_Text levelHeaderText;
        public TMP_Text levelLostText;
        public Button repeatLevelButton;
        [Header("Settings")]
        public string levelHeaderFormat = "LEVEL {0} FAILED";

        public override void InitUI()
        {
            LevelScene levelScene = GameManager.Instance.CurrentLevel;
            levelHeaderText.text = string.Format(levelHeaderFormat, GameManager.Instance.CurrentLevelIndex + 1);
            levelLostText.text = levelScene.loseText;


            repeatLevelButton.onClick.RemoveAllListeners();
            repeatLevelButton.onClick.AddListener(() =>
            {
                ScreenManager.Instance.CloseAllScreens();
                GameManager.Instance.ResetCurrentLevel();
            });
        }
    }
    
}

