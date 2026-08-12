using Sirenix.OdinInspector;
using System.Collections;
using UnityEngine; using Game.EventManagement;
using UnityEngine.UI;
using TMPro;
using Game.Ads;

namespace Game
{
    public class LoseScreen : GameScreen
    {
        public override Screens ScreenID => Screens.LoseScreen;

        public TMP_Text levelHeaderText;
        public TMP_Text addMovesCost;
        public Button addMovesButton;
        public Button repeatLevelButton;
        public TMP_Text repeatLevelText;
        public CurrencyModel heartCurrency;
        [Header("Settings")]
        public string levelHeaderFormat = "LEVEL {0} FAILED";
        public string addMovesCostFormat = "{0}<sprite index=1>";
        public string repeatLevelButtonFormat = "TRY AGAIN\n<sprite index={0}> -{1}";
        public string buyHeartsButtonFormat = "BUY {0} HEARTS";
        public string buyHeartPromptText = "Retrying this level will cost {0} hearts and you will lose your progress. Continue?";
        [Min(1)] public int retryHeartCost = 1;
        [Min(1)] public int rewardedHeartAmount = 3;

        public override void InitUI(EventParam eventParam)
        {
            base.InitUI(eventParam);
            LevelScene_Match3Game levelScene = GameManager.Instance.CurrentLevel as LevelScene_Match3Game;
            if(levelHeaderText) levelHeaderText.text = string.Format(levelHeaderFormat, GameManager.Instance.CurrentLevelIndex + 1);
            if(addMovesCost) addMovesCost.text = string.Format(addMovesCostFormat, levelScene.extraMoveCost.amount, levelScene.extraMoveCost.type.spriteIndexForUI);

            addMovesButton.interactable = CanAddMovesOrTime();

            addMovesButton.onClick.RemoveAllListeners();
            addMovesButton.onClick.AddListener(BuyMovesAndShowBoosterSelection);

            repeatLevelButton.onClick.RemoveAllListeners();
            repeatLevelButton.onClick.AddListener(OnRepeatLevelButtonClicked);
            UpdateRepeatLevelButton();
        }
        public override void ResolveParams(EventParam eventParam)
        {
            
        }

        private bool CanAddMovesOrTime()
        {
            LevelScene_Match3Game levelScene = GameManager.Instance.CurrentLevel as LevelScene_Match3Game;
            return levelScene != null && levelScene.CanBuyExtraMovesOrTime();
        }

        private void OnDisable()
        {
            UnsubscribeFromRewardedAdEvents();
        }

        private void OnRepeatLevelButtonClicked()
        {
            if (HasEnoughHearts())
            {
                if (CanAddMovesOrTime())
                {
                    ShowRetryOrBuyMovesPrompt();
                }
                else
                {
                    RetryLevel();
                }
                return;
            }

            ShowHeartRefillRewardedAd();
        }

        private void ShowRetryOrBuyMovesPrompt()
        {
            LevelScene_Match3Game levelScene = GameManager.Instance.CurrentLevel as LevelScene_Match3Game;
            if (levelScene == null)
            {
                RetryLevel();
                return;
            }

            string buyMovesText = string.Format(
                addMovesCostFormat,
                levelScene.extraMoveCost.amount,
                levelScene.extraMoveCost.type.spriteIndexForUI);

            PromptScreen.ShowPrompt(
                string.Empty,
                string.Format(buyHeartPromptText, retryHeartCost),
                ("OK", RetryLevel),
                (buyMovesText, BuyMovesAndShowBoosterSelection));
        }

        private void RetryLevel()
        {
            CurrencyManager.Instance.AddCurrency(heartCurrency, -retryHeartCost);
            ScreenManager.Instance.CloseAllNonPersistentScreens();
            EventManager.TriggerEvent(GameEvent.LEVEL_EXTRA_MOVE_REJECTED);
            GameManager.Instance.ResetCurrentLevel();
        }

        private void BuyMovesAndShowBoosterSelection()
        {
            LevelScene_Match3Game levelScene = GameManager.Instance.CurrentLevel as LevelScene_Match3Game;
            if (levelScene == null || !levelScene.CanBuyExtraMovesOrTime())
                return;

            ScreenManager.Instance.CloseAllNonPersistentScreens();
            levelScene.BuyExtraMovesOrTime();
            // Restore the game state to what it was before the lose condition was triggered, so that the player can continue playing after buying extra moves or time.
            levelScene.RestoreStateBeforeLoseCondition();
            ShowBoosterSelectionScreen();
        }

        private void ShowBoosterSelectionScreen()
        {
            Match3Grid grid = (GameManager.Instance.CurrentLevel as LevelScene_Match3Game)?.grid as Match3Grid;
            if (grid == null || !grid.HasAvailableBoosterPlacement())
                return;

            GameScreen boosterSelectionScreen = ScreenManager.Instance.screens.Find(
                screen => screen != null && screen.ScreenID == Screens.BoosterSelection);
            if (boosterSelectionScreen == null)
            {
                Debug.LogWarning("BoosterSelectionScreen was not found by ScreenManager. Add the Booster Selection Panel to the scene.");
                return;
            }

            ScreenManager.Instance.Show(boosterSelectionScreen);
        }

        private void ShowHeartRefillRewardedAd()
        {
            UnityAdsManager adsManager = UnityAdsManager.Instance;
            if (adsManager == null)
            {
                Debug.LogError("UnityAdsManager instance not found. Cannot show heart refill rewarded ad.");
                return;
            }

            repeatLevelButton.interactable = false;
            adsManager.RewardedAdClosed += OnRewardedAdClosed;
            adsManager.RewardedAdFailedToShow += OnRewardedAdFailedToShow;

            if (!adsManager.ShowRewardedAd(RewardType.HeartRefill))
            {
                UnsubscribeFromRewardedAdEvents();
                repeatLevelButton.interactable = true;
                Debug.Log("Rewarded ad not ready");
            }
        }

        private void OnRewardedAdClosed(RewardedAdResult result)
        {
            if (result.RewardType != RewardType.HeartRefill)
                return;

            UnsubscribeFromRewardedAdEvents();
            repeatLevelButton.interactable = true;
            if (result.IsGranted)
                StartCoroutine(GrantHeartsAndRefreshButton());
            else
                UpdateRepeatLevelButton();
        }

        private void OnRewardedAdFailedToShow(RewardedAdResult result)
        {
            if (result.RewardType != RewardType.HeartRefill)
                return;

            UnsubscribeFromRewardedAdEvents();
            repeatLevelButton.interactable = true;
        }

        private bool HasEnoughHearts()
        {
            return heartCurrency != null &&
                   CurrencyManager.Instance != null &&
                   CurrencyManager.Instance.GetCurrencyAmount(heartCurrency) >= retryHeartCost;
        }

        private IEnumerator GrantHeartsAndRefreshButton()
        {
            yield return StartCoroutine(CurrencyManager.Instance.AddCurrencyCoroutine(heartCurrency, rewardedHeartAmount));
            UpdateRepeatLevelButton();
        }

        private void UpdateRepeatLevelButton()
        {
            if (repeatLevelText == null)
                return;

            repeatLevelText.text = HasEnoughHearts()
                ? string.Format(repeatLevelButtonFormat, heartCurrency.spriteIndexForUI, retryHeartCost)
                : string.Format(buyHeartsButtonFormat, rewardedHeartAmount);
        }

        private void UnsubscribeFromRewardedAdEvents()
        {
            UnityAdsManager adsManager = UnityAdsManager.Instance;
            if (adsManager == null)
                return;

            adsManager.RewardedAdClosed -= OnRewardedAdClosed;
            adsManager.RewardedAdFailedToShow -= OnRewardedAdFailedToShow;
        }
    }
}

