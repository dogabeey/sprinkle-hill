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
        public TMP_Text levelLostReason;
        public TMP_Text addMovesText;
        public TMP_Text addMovesCost;
        public Button addMovesButton;
        public Button repeatLevelButton;
        [Header("Settings")]
        public string levelHeaderFormat = "LEVEL {0} FAILED";
        public string addMovesButtonFormat = "+{0} MOVES";
        public string addMovesCostFormat = "{0}<sprite index=1>";

        public override void InitUI(EventParam eventParam)
        {
            base.InitUI(eventParam);
            LevelScene_Match3Game levelScene = GameManager.Instance.CurrentLevel as LevelScene_Match3Game;
            if(levelHeaderText) levelHeaderText.text = string.Format(levelHeaderFormat, GameManager.Instance.CurrentLevelIndex + 1);
            if(levelLostReason) levelLostReason.text = "OUT OF MOVES!";
            if(addMovesText) addMovesText.text = string.Format(addMovesButtonFormat, levelScene.extraMovesGiven);
            if(addMovesCost) addMovesCost.text = string.Format(addMovesCostFormat, levelScene.extraMoveCost.amount, levelScene.extraMoveCost.type.spriteIndexForUI);

            addMovesButton.interactable = CanAddMovesOrTime();

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
        public override void ResolveParams(EventParam eventParam)
        {
            
        }

        private bool CanAddMovesOrTime()
        {
            LevelScene_Match3Game levelScene = GameManager.Instance.CurrentLevel as LevelScene_Match3Game;
            return levelScene != null && levelScene.CanBuyExtraMovesOrTime();
        }
    }
}

