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

        private List<TMP_Text> rewards = new List<TMP_Text>();

        public override void InitUI()
        {
            LevelScene levelScene = GameManager.Instance.CurrentLevel;
            levelHeaderText.text = levelScene.levelName;
            levelWinText.text = levelScene.winText;

            // Remove old rewards
            foreach (var reward in rewards)
            {
                Destroy(reward.gameObject);
            }
            rewards.Clear();
            foreach (var reward in levelScene.rewards)
            {
                var rewardText = Instantiate(rewardTextPrefab, levelRewardContainer);
                rewards.Add(rewardText);
                rewardText.text = $"<sprite index={reward.type.spriteIndexForUI}> {reward.amount}";
            }

            nextLevelButton.onClick.RemoveAllListeners();
            nextLevelButton.onClick.AddListener(() =>
            {
                levelScene.rewards.ForEach(r => CurrencyManager.Instance.AddCurrency(r.type.currencyID, r.amount));

                ScreenManager.Instance.Show(Screens.FeatureProgress);
            });
        }
    }
    
}

