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
        public Button addMovesButton;
        public Button repeatLevelButton;
        [Header("Settings")]
        public string levelHeaderFormat = "LEVEL {0} FAILED";

        public override void InitUI()
        {
            LevelScene levelScene = GameManager.Instance.CurrentLevel;
            levelHeaderText.text = string.Format(levelHeaderFormat, GameManager.Instance.CurrentLevelIndex + 1);
            levelLostText.text = levelScene.loseText;

            addMovesButton.interactable = CanAddMoves();

            addMovesButton.onClick.RemoveAllListeners();
            addMovesButton.onClick.AddListener(() =>
            {
                ScreenManager.Instance.CloseAllScreens();
                (GameManager.Instance.CurrentLevel as LevelScene_Match3Game).BuyExtraMovesOrTime();
        });

            repeatLevelButton.onClick.RemoveAllListeners();
            repeatLevelButton.onClick.AddListener(() =>
            {
                ScreenManager.Instance.CloseAllScreens();
                GameManager.Instance.ResetCurrentLevel();
            });
        }

        private bool CanAddMoves()
        {
            LevelScene_Match3Game levelScene = GameManager.Instance.CurrentLevel as LevelScene_Match3Game;
            return levelScene != null && levelScene.CanBuyExtraMovesOrTime();
        }
    }
}

