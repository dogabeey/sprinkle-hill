using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game
{
    public class WinScreen : GameScreen
    {
        public override Screens ScreenID => Screens.WinScreen;

        public TMP_Text levelHeaderText;
        public TMP_Text levelWinText;
        public Transform levelRewardContainer;
        [AssetsOnly] public TMP_Text rewardTextPrefab;
        public Button nextLevelButton;
        [Header("Settings")]
        public string levelHeaderFormat = "DAY {0} RESULTS";
        public string rewardTextFormat = "<sprite index={0}>\n{1}";

        private List<TMP_Text> rewardTexts = new List<TMP_Text>();

        public override void InitUI()
        {
            LevelScene levelScene = GameManager.Instance.CurrentLevel;
            if (levelHeaderText) levelHeaderText.text = string.Format(levelHeaderFormat, GameManager.Instance.CurrentLevelIndex + 1);
            if(levelWinText) levelWinText.text = levelScene.winText;

            // Remove old rewards
            foreach (var reward in rewardTexts)
            {
                Destroy(reward.gameObject);
            }
            rewardTexts.Clear();
            foreach (var reward in levelScene.rewards)
            {
                var rewardText = Instantiate(rewardTextPrefab, levelRewardContainer);
                rewardTexts.Add(rewardText);
                rewardText.text = string.Format(rewardTextFormat, reward.type.spriteIndexForUI, reward.amount);
            }

            nextLevelButton.onClick.RemoveAllListeners();
            nextLevelButton.onClick.AddListener(() =>
            {
                OnNextLevelButtonClicked();
            });
        }

        private void OnNextLevelButtonClicked()
        {
            StartCoroutine(OnNextLevelButtonClickedCoroutine());
        }
        private IEnumerator OnNextLevelButtonClickedCoroutine()
        {
            LevelScene levelScene = GameManager.Instance.CurrentLevel;
            foreach (var reward in levelScene.rewards)
            {
                // Get reward text for the current reward to use as the source for the flying currency animation
                GameObject sourceObject = rewardTexts[levelScene.rewards.IndexOf(reward)].gameObject;
                yield return StartCoroutine(CurrencyManager.Instance.AddCurrencyCoroutine(reward.type.currencyID, reward.amount, sourceObject));
            }

            ScreenManager.Instance.CloseAllScreens();
            if (GameManager.Instance.showFeatureProgressScreen)
            {
                ScreenManager.Instance.Show(Screens.FeatureProgress);
            }
            else
            {
                GameManager.Instance.LoadNextLevel();
            }
        }

    }
    
}

