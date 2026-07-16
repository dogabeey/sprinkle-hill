using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine; using Game.EventManagement;
using UnityEngine.UI;
using Game.Ads;


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
        public Button extraRewardButton;
        [Header("Settings")]
        public string levelHeaderFormat = "DAY {0} RESULTS";
        public string rewardTextFormat = "<sprite index={0}>\n{1}";

        private List<TMP_Text> rewardTexts = new List<TMP_Text>();

        public override void InitUI(EventParam eventParam)
        {
            base.InitUI(eventParam);
            LevelScene levelScene = GameManager.Instance.CurrentLevel;
            if (levelHeaderText) levelHeaderText.text = string.Format(levelHeaderFormat, GameManager.Instance.CurrentLevelIndex + 1);
            if (levelWinText) levelWinText.text = levelScene.winText;


            nextLevelButton.interactable = true;
            extraRewardButton.interactable = true;

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
                nextLevelButton.interactable = false;
                OnNextLevelButtonClicked();
            });
            extraRewardButton.onClick.RemoveAllListeners();
            extraRewardButton.onClick.AddListener(() =>
            {
                extraRewardButton.interactable = false;
                OnExtraRewardButtonClicked();
            });
        }

        private void OnNextLevelButtonClicked()
        {
            StartCoroutine(OnNextLevelButtonClickedCoroutine());
        }

        private void OnExtraRewardButtonClicked()
        {
            UnityAdsManager adsManager = UnityAdsManager.Instance;
            adsManager.RewardedAdClosed += OnRewardedAdClosed;
            adsManager.RewardedAdFailedToShow += OnRewardedAdFailedToShow;

            if (adsManager.ShowRewardedAd(RewardType.ExtraLevelEndReward))
            {
                nextLevelButton.interactable = false;
            }
            else
            {
                UnsubscribeFromRewardedAdEvents(adsManager);
                Debug.Log("Rewarded ad not ready");
                extraRewardButton.interactable = true;
            }
        }

        private void OnRewardedAdClosed(RewardedAdResult result)
        {
            if (result.RewardType != RewardType.ExtraLevelEndReward)
                return;

            UnsubscribeFromRewardedAdEvents(UnityAdsManager.Instance);
            float rewardMultiplier = result.IsGranted ? ConstantManager.Instance.adRewardMultiplier : 1f;
            StartCoroutine(OnNextLevelButtonClickedCoroutine(rewardMultiplier));
        }

        private void OnRewardedAdFailedToShow(RewardedAdResult result)
        {
            if (result.RewardType != RewardType.ExtraLevelEndReward)
                return;

            UnsubscribeFromRewardedAdEvents(UnityAdsManager.Instance);
            nextLevelButton.interactable = true;
            extraRewardButton.interactable = true;
        }

        private void UnsubscribeFromRewardedAdEvents(UnityAdsManager adsManager)
        {
            adsManager.RewardedAdClosed -= OnRewardedAdClosed;
            adsManager.RewardedAdFailedToShow -= OnRewardedAdFailedToShow;
        }

        private IEnumerator OnNextLevelButtonClickedCoroutine(float rewardMultiplier = 1f)
        {
            LevelScene levelScene = GameManager.Instance.CurrentLevel;
            foreach (var reward in levelScene.rewards)
            {
                // Get reward text for the current reward to use as the source for the flying currency animation
                GameObject sourceObject = rewardTexts[levelScene.rewards.IndexOf(reward)].gameObject;
                int rewardAmount = Mathf.RoundToInt(reward.amount * rewardMultiplier);
                yield return StartCoroutine(CurrencyManager.Instance.AddCurrencyCoroutine(reward.type, rewardAmount, sourceObject));
            }

            ScreenManager.Instance.CloseAllNonPersistentScreens();
            if (GameManager.Instance.showFeatureProgressScreen)
            {
                ScreenManager.Instance.Show(Screens.FeatureProgress);
            }
            else
            {
                GameManager.Instance.LoadNextLevel();
            }
        }
        public override void ResolveParams(EventParam eventParam)
        {

        }

    }

}