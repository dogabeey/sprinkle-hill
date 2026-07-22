using System;
using Game.EventManagement;
using Game.Singleton;
using GoogleMobileAds.Api;
using UnityEngine;
using UnityEngine.Events;

namespace Game.Ads
{
    public enum RewardType
    {
        ExtraLevelEndReward,
        BoosterReward,
        HeartRefill,
    }

    public sealed class RewardedAdResult
    {
        public RewardType RewardType { get; }
        public bool IsGranted { get; }

        public RewardedAdResult(RewardType rewardType, bool isGranted)
        {
            RewardType = rewardType;
            IsGranted = isGranted;
        }
    }

    /// <summary>
    /// Owns the Google Mobile Ads (AdMob) ad lifecycle for the game.
    /// Assign production unit IDs in the Inspector before publishing.
    /// </summary>
    public class UnityAdsManager : SingletonComponent<UnityAdsManager>
    {
        [Header("Interstitial timing")]
        [Min(0f)] public float adInterval = 300f;
        [Min(0)] public int firstLevelInterval = 10;
        [Min(0)] public int levelInterval = 2;

        [Header("AdMob unit IDs")]
        [SerializeField] private string androidInterstitialAdUnitId = "ca-app-pub-5053129562534405/6063909105" ;
        [SerializeField] private string androidRewardedAdUnitId = "ca-app-pub-5053129562534405/9605101235";
        [SerializeField] private string androidBannerAdUnitId = "ca-app-pub-5053129562534405/8439479361";
        [SerializeField] private string iosInterstitialAdUnitId = "ca-app-pub-3940256099942544/4411468910"; // Not implement yet. Fake test id.
        [SerializeField] private string iosRewardedAdUnitId = "ca-app-pub-3940256099942544/1712485313"; // Not implement yet. Fake test id.
        [SerializeField] private string iosBannerAdUnitId = "ca-app-pub-3940256099942544/2934735716"; // Not implement yet. Fake test id.

        [Header("Banner")]
        [SerializeField] private bool loadBannerOnInitialize = true;
        [SerializeField] private AdPosition bannerPosition = AdPosition.Bottom;

        // Error events expose an error message rather than a provider-specific error type.
        public UnityEvent<object, EventArgs> onAdClosedEvent = new();
        public UnityEvent<object, string> onAdFailedShowEvent = new();
        public UnityEvent<object, EventArgs> onAdLoadedEvent = new();
        public UnityEvent<object, string> onAdFailedLoadEvent = new();
        public UnityEvent<object, EventArgs> onAdClickedEvent = new();
        public UnityEvent<object, EventArgs> onRewardedClosedEvent = new();
        public UnityEvent<object, string> onRewardedFailedShowEvent = new();
        public UnityEvent<object, EventArgs> onRewardedLoadedEvent = new();
        public UnityEvent<object, string> onRewardedFailedLoadEvent = new();
        public UnityEvent<object, EventArgs> onRewardedClickedEvent = new();
        public UnityEvent<object, EventArgs> onBannerClickedEvent = new();

        public event Action<RewardedAdResult> RewardedAdShown;
        public event Action<RewardedAdResult> RewardedAdGranted;
        public event Action<RewardedAdResult> RewardedAdClosed;
        public event Action<RewardedAdResult> RewardedAdFailedToShow;

        private InterstitialAd interstitialAd;
        private RewardedAd rewardedAd;
        private BannerView bannerView;
        private string interstitialAdUnitId;
        private string rewardedAdUnitId;
        private string bannerAdUnitId;
        private float timeSinceLastAd;
        private int levelsSinceLastAd;
        private bool isInitialized;
        private bool rewardedGrantedInCurrentShow;
        private RewardType currentRewardType;

        private void OnEnable()
        {
            EventManager.StartListening(GameEvent.LEVEL_STARTED, OnLevelStarted);
            EventManager.StartListening(GameEvent.LEVEL_COMPLETED, OnLevelCompleted);
            EventManager.StartListening(GameEvent.LEVEL_EXTRA_MOVE_REJECTED, OnLevelCompleted);
        }

        private void OnDisable()
        {
            EventManager.StopListening(GameEvent.LEVEL_STARTED, OnLevelStarted);
            EventManager.StopListening(GameEvent.LEVEL_COMPLETED, OnLevelCompleted);
            EventManager.StopListening(GameEvent.LEVEL_EXTRA_MOVE_REJECTED, OnLevelCompleted);
        }

        private void Start()
        {
            timeSinceLastAd = 0f;
            ConfigureAdUnitIds();
            MobileAds.Initialize(OnMobileAdsInitialized);
        }

        private void Update()
        {
            timeSinceLastAd += Time.deltaTime;
        }

        private void OnLevelStarted(EventParam _)
        {
            levelsSinceLastAd++;
        }

        private void OnLevelCompleted(EventParam e)
        {
            if (e.paramInt >= firstLevelInterval)
                TryShowAd();
        }

        private void ConfigureAdUnitIds()
        {
#if UNITY_IOS
            interstitialAdUnitId = iosInterstitialAdUnitId;
            rewardedAdUnitId = iosRewardedAdUnitId;
            bannerAdUnitId = iosBannerAdUnitId;
#else
            interstitialAdUnitId = androidInterstitialAdUnitId;
            rewardedAdUnitId = androidRewardedAdUnitId;
            bannerAdUnitId = androidBannerAdUnitId;
#endif
        }

        private void OnMobileAdsInitialized(InitializationStatus _)
        {
            isInitialized = true;
            LoadInterstitialAd();
            LoadRewardedAd();

            if (loadBannerOnInitialize)
                LoadBannerAd();
        }

        public void TryShowAd()
        {
            if (timeSinceLastAd < adInterval || levelsSinceLastAd <= levelInterval || interstitialAd == null || !interstitialAd.CanShowAd())
                return;

            ShowAd();
        }

        public void ShowAd()
        {
            if (interstitialAd == null || !interstitialAd.CanShowAd())
            {
                Debug.Log("Interstitial ad is not ready.");
                return;
            }

            timeSinceLastAd = 0f;
            levelsSinceLastAd = 0;
            interstitialAd.Show();
            EventManager.TriggerEvent(GameEvent.AD_SHOWN);
        }

        public bool ShowRewardedAd(RewardType rewardType = RewardType.BoosterReward)
        {
            if (rewardedAd == null || !rewardedAd.CanShowAd())
            {
                Debug.Log("Rewarded ad is not ready.");
                return false;
            }

            currentRewardType = rewardType;
            rewardedGrantedInCurrentShow = false;
            rewardedAd.Show(OnUserEarnedReward);
            RewardedAdShown?.Invoke(new RewardedAdResult(currentRewardType, false));
            EventManager.TriggerEvent(GameEvent.REWARDED_AD_SHOWN);
            return true;
        }

        public void LoadInterstitialAd()
        {
            if (!isInitialized || string.IsNullOrWhiteSpace(interstitialAdUnitId))
                return;

            DestroyInterstitialAd();
            InterstitialAd.Load(interstitialAdUnitId, new AdRequest(), (ad, error) =>
            {
                if (error != null)
                {
                    ReportInterstitialLoadFailure(error);
                    return;
                }

                interstitialAd = ad;
                RegisterInterstitialCallbacks(ad);
                onAdLoadedEvent.Invoke(this, EventArgs.Empty);
            });
        }

        public void LoadRewardedAd()
        {
            if (!isInitialized || string.IsNullOrWhiteSpace(rewardedAdUnitId))
                return;

            DestroyRewardedAd();
            RewardedAd.Load(rewardedAdUnitId, new AdRequest(), (ad, error) =>
            {
                if (error != null)
                {
                    ReportRewardedLoadFailure(error);
                    return;
                }

                rewardedAd = ad;
                RegisterRewardedCallbacks(ad);
                onRewardedLoadedEvent.Invoke(this, EventArgs.Empty);
            });
        }

        public void LoadBannerAd()
        {
            if (!isInitialized || string.IsNullOrWhiteSpace(bannerAdUnitId))
                return;

            DestroyBannerAd();
            int safeDeviceWidth = MobileAds.Utils.GetDeviceSafeWidth();
            AdSize adaptiveBannerSize = AdSize.GetCurrentOrientationAnchoredAdaptiveBannerAdSizeWithWidth(safeDeviceWidth);
            bannerView = new BannerView(bannerAdUnitId, adaptiveBannerSize, bannerPosition);
            bannerView.OnBannerAdLoaded += OnBannerAdLoaded;
            bannerView.OnBannerAdLoadFailed += OnBannerAdLoadFailed;
            bannerView.OnAdClicked += OnBannerAdClicked;
            bannerView.LoadAd(new AdRequest());
        }

        private void RegisterInterstitialCallbacks(InterstitialAd ad)
        {
            ad.OnAdFullScreenContentClosed += OnInterstitialClosed;
            ad.OnAdFullScreenContentFailed += OnInterstitialFailedToShow;
            ad.OnAdClicked += OnInterstitialClicked;
        }

        private void RegisterRewardedCallbacks(RewardedAd ad)
        {
            ad.OnAdFullScreenContentClosed += OnRewardedClosed;
            ad.OnAdFullScreenContentFailed += OnRewardedFailedToShow;
            ad.OnAdClicked += OnRewardedClicked;
        }

        private void OnInterstitialClosed()
        {
            onAdClosedEvent.Invoke(this, EventArgs.Empty);
            EventManager.TriggerEvent(GameEvent.AD_CLOSED);
            LoadInterstitialAd();
        }

        private void OnInterstitialFailedToShow(AdError error)
        {
            string message = error?.GetMessage() ?? "Unknown interstitial show error.";
            Debug.LogError($"Interstitial ad failed to show: {message}");
            onAdFailedShowEvent.Invoke(this, message);
            EventManager.TriggerEvent(GameEvent.AD_FAILED, new EventParam(paramStr: message));
            LoadInterstitialAd();
        }

        private void OnInterstitialClicked()
        {
            onAdClickedEvent.Invoke(this, EventArgs.Empty);
        }

        private void OnRewardedClosed()
        {
            onRewardedClosedEvent.Invoke(this, EventArgs.Empty);
            RewardedAdResult result = new(currentRewardType, rewardedGrantedInCurrentShow);
            RewardedAdClosed?.Invoke(result);

            if (rewardedGrantedInCurrentShow)
                EventManager.TriggerEvent(GameEvent.REWARDED_AD_COMPLETED);

            LoadRewardedAd();
        }

        private void OnRewardedFailedToShow(AdError error)
        {
            string message = error?.GetMessage() ?? "Unknown rewarded show error.";
            Debug.LogError($"Rewarded ad failed to show: {message}");
            onRewardedFailedShowEvent.Invoke(this, message);
            RewardedAdFailedToShow?.Invoke(new RewardedAdResult(currentRewardType, false));
            EventManager.TriggerEvent(GameEvent.REWARDED_AD_FAILED, new EventParam(paramStr: message));
            LoadRewardedAd();
        }

        private void OnRewardedClicked()
        {
            onRewardedClickedEvent.Invoke(this, EventArgs.Empty);
        }

        private void OnUserEarnedReward(Reward _)
        {
            rewardedGrantedInCurrentShow = true;
            RewardedAdGranted?.Invoke(new RewardedAdResult(currentRewardType, true));
        }

        private void OnBannerAdLoaded()
        {
            EventManager.TriggerEvent(GameEvent.BANNER_AD_LOADED, new EventParam(paramFloat: bannerView.GetHeightInPixels()));
        }

        private void OnBannerAdLoadFailed(LoadAdError error)
        {
            Debug.LogError($"Banner ad failed to load: {error?.GetMessage() ?? "Unknown error."}");
        }

        private void OnBannerAdClicked()
        {
            onBannerClickedEvent.Invoke(this, EventArgs.Empty);
        }

        private void ReportInterstitialLoadFailure(LoadAdError error)
        {
            string message = error?.GetMessage() ?? "Unknown interstitial load error.";
            Debug.LogError($"Interstitial ad failed to load: {message}");
            onAdFailedLoadEvent.Invoke(this, message);
        }

        private void ReportRewardedLoadFailure(LoadAdError error)
        {
            string message = error?.GetMessage() ?? "Unknown rewarded load error.";
            Debug.LogError($"Rewarded ad failed to load: {message}");
            onRewardedFailedLoadEvent.Invoke(this, message);
        }

        private void OnDestroy()
        {
            DestroyInterstitialAd();
            DestroyRewardedAd();
            DestroyBannerAd();
        }

        private void DestroyInterstitialAd()
        {
            interstitialAd?.Destroy();
            interstitialAd = null;
        }

        private void DestroyRewardedAd()
        {
            rewardedAd?.Destroy();
            rewardedAd = null;
        }

        private void DestroyBannerAd()
        {
            if (bannerView == null)
                return;

            bannerView.OnBannerAdLoaded -= OnBannerAdLoaded;
            bannerView.OnBannerAdLoadFailed -= OnBannerAdLoadFailed;
            bannerView.OnAdClicked -= OnBannerAdClicked;
            bannerView.Destroy();
            bannerView = null;
        }
    }
}
