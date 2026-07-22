using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Game.Ads;
using Game.EventManagement;

namespace Game
{
    public class MainMenuPanel : MonoBehaviour
    {

        [SerializeField] private Button currentLevelButton;
        [SerializeField] private TMP_Text currentLevelText;
        [SerializeField] private CurrencyModel heartCurrency;
        [SerializeField, Min(1)] private int levelStartHeartCost = 1;
        [SerializeField, Min(1)] private int rewardedHeartAmount = 3;
        [SerializeField] private string currentLevelTextFormat = "DAY {0}";
        [SerializeField] private string outOfHeartsTextFormat = "Buy {0} Hearts to continue playing";
        [SerializeField] private Button marketButton;
        [SerializeField] private Button leaderboardButton;
        [SerializeField] private Button homeButton;
        [SerializeField] private Button missionsButton;
        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            currentLevelButton.onClick.AddListener(OnCurrentLevelButtonClicked);
            marketButton.onClick.AddListener(OnMarketButtonClicked);
            leaderboardButton.onClick.AddListener(OnLeaderboardButtonClicked);
            homeButton.onClick.AddListener(OnHomeButtonClicked);
            missionsButton.onClick.AddListener(OnMissionsButtonClicked);
        }
        void OnEnable()
        {
            CurrencyManager.Instance.currencyCanvasGroup.alpha = 1f;
            EventManager.StartListening(GameEvent.CURRENCY_CHANGED, OnCurrencyChanged);
            UpdateCurrentLevelText();
        }

        private void OnDisable()
        {
            EventManager.StopListening(GameEvent.CURRENCY_CHANGED, OnCurrencyChanged);
            UnsubscribeFromRewardedAdEvents();
        }
        
        private void OnCurrentLevelButtonClicked()
        {
            if (!HasEnoughHearts())
            {
                ShowHeartRefillRewardedAd();
                return;
            }

            CurrencyManager.Instance.AddCurrency(heartCurrency, -levelStartHeartCost);
            GameManager.Instance.ChangeGameState(GameState.Level);
        }

        private void ShowHeartRefillRewardedAd()
        {
            UnityAdsManager adsManager = UnityAdsManager.Instance;
            if (adsManager == null)
            {
                Debug.LogError("UnityAdsManager instance not found. Cannot show heart refill rewarded ad.");
                return;
            }

            currentLevelButton.interactable = false;
            adsManager.RewardedAdClosed += OnRewardedAdClosed;
            adsManager.RewardedAdFailedToShow += OnRewardedAdFailedToShow;

            if (!adsManager.ShowRewardedAd(RewardType.HeartRefill))
            {
                UnsubscribeFromRewardedAdEvents();
                currentLevelButton.interactable = true;
                Debug.Log("Rewarded ad not ready");
            }
        }

        private void OnRewardedAdClosed(RewardedAdResult result)
        {
            if (result.RewardType != RewardType.HeartRefill)
                return;

            UnsubscribeFromRewardedAdEvents();
            currentLevelButton.interactable = true;
            if (result.IsGranted)
                StartCoroutine(GrantHearts());
        }

        private void OnRewardedAdFailedToShow(RewardedAdResult result)
        {
            if (result.RewardType != RewardType.HeartRefill)
                return;

            UnsubscribeFromRewardedAdEvents();
            currentLevelButton.interactable = true;
        }

        private bool HasEnoughHearts()
        {
            return heartCurrency != null &&
                   CurrencyManager.Instance != null &&
                   CurrencyManager.Instance.GetCurrencyAmount(heartCurrency) >= levelStartHeartCost;
        }

        private IEnumerator GrantHearts()
        {
            yield return StartCoroutine(CurrencyManager.Instance.AddCurrencyCoroutine(heartCurrency, rewardedHeartAmount));
            UpdateCurrentLevelText();
        }

        private void OnCurrencyChanged(EventParam eventParam)
        {
            if (eventParam.paramScriptable == heartCurrency)
                UpdateCurrentLevelText();
        }

        private void UpdateCurrentLevelText()
        {
            if (currentLevelText == null)
                return;

            currentLevelText.text = HasEnoughHearts()
                ? string.Format(currentLevelTextFormat, GameManager.Instance.CurrentLevelIndex + 1)
                : string.Format(outOfHeartsTextFormat, rewardedHeartAmount);
        }

        private void UnsubscribeFromRewardedAdEvents()
        {
            UnityAdsManager adsManager = UnityAdsManager.Instance;
            if (adsManager == null)
                return;

            adsManager.RewardedAdClosed -= OnRewardedAdClosed;
            adsManager.RewardedAdFailedToShow -= OnRewardedAdFailedToShow;
        }
        private void OnMarketButtonClicked()
        {
            ScreenManager.Instance.Show(Screens.Market);
        }
        private void OnLeaderboardButtonClicked()
        {
            ScreenManager.Instance.Show(Screens.Leaderboard);
        }
        private void OnHomeButtonClicked()
        {
            ScreenManager.Instance.CloseAllNonPersistentScreens();
        }
        private void OnMissionsButtonClicked()
        {
            ScreenManager.Instance.Show(Screens.Missions);
        }
        // Update is called once per frame
        void Update()
        {
            
        }
    }
}
