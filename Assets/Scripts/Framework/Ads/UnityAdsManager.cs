using System;
using UnityEngine;
using Unity.Services.Core;
using Unity.Services.LevelPlay;
using Unity.Services.RemoteConfig;
using UnityEngine.Events;
using Game;
using System.Threading.Tasks;

public partial class UnityAdsManager : SingletonComponent<UnityAdsManager>
{
    public float adInterval = 300.0f; // Time interval between ads in seconds
    public int levelInterval = 2; // Time interval between ads in seconds
    public int bannerHeight;

    internal UnityEvent<object, EventArgs> onAdClosedEvent = new();
    internal UnityEvent<object, LevelPlayAdError> onAdFailedShowEvent = new();
    internal UnityEvent<object, EventArgs> onAdLoadedEvent = new();
    internal UnityEvent<object, LevelPlayAdError> onAdFailedLoadEvent = new();
    internal UnityEvent<object, EventArgs> onAdClickedEvent = new();
    internal UnityEvent<object, EventArgs> onRewardedClosedEvent = new();
    internal UnityEvent<object, LevelPlayAdError> onRewardedFailedShowEvent = new();
    internal UnityEvent<object, EventArgs> onRewardedLoadedEvent = new();
    internal UnityEvent<object, LevelPlayAdError> onRewardedFailedLoadEvent = new();
    internal UnityEvent<object, EventArgs> onRewardedClickedEvent = new();
    internal UnityEvent<object, EventArgs> onBannerClickedEvent = new();


    private string gameId;
    private string IS_adUnitId;
    private string RW_adUnitId;
    private string Banner_adUnitId;
    private float timeSinceLastAd;
    private ILevelPlayInterstitialAd interstitialAd;
    private ILevelPlayRewardedAd rewardedAd;
    private ILevelPlayBannerAd bannerAd;
    private int levelsSinceLastAd = 0;
    private bool isLevelPlayInitialized;
    private bool rewardedGrantedInCurrentShow;

    private void OnEnable()
    {
        EventManager.StartListening(GameEvent.LEVEL_STARTED, OnLevelStarted);
    }
    private void OnDisable()
    {
        EventManager.StopListening(GameEvent.LEVEL_STARTED, OnLevelStarted);
    }
    private void OnLevelStarted(EventParam e)
    {
        levelsSinceLastAd++;
    }

    async void Start()
    {
        timeSinceLastAd = 0.0f;

#if UNITY_ANDROID
        gameId = "6118896"; // Replace with your actual Android Game ID
        IS_adUnitId = "Interstitial_Android"; // Replace with your actual Android Ad Unit ID
        RW_adUnitId = "Rewarded_Android"; // Replace with your actual Android Ad Unit ID
        Banner_adUnitId = "Banner_Android"; // Replace with your actual Android Ad Unit ID
#elif UNITY_IOS
        gameId = "6118897"; // Replace with your actual iOS Game ID
        IS_adUnitId = "Interstitial_iOS"; // Replace with your actual iOS Ad Unit ID
        RW_adUnitId = "Rewarded_iOS"; // Replace with your actual iOS Ad Unit ID
        Banner_adUnitId = "Banner_iOS"; // Replace with your actual iOS Ad Unit ID
#else
        Debug.LogError("Unsupported platform");
        return;
#endif

        try
        {
            await UnityServices.InitializeAsync();

            LevelPlay.OnInitSuccess += OnLevelPlayInitSuccess;
            LevelPlay.OnInitFailed += OnLevelPlayInitFailed;
            LevelPlay.Init(gameId);
        }
        catch (Exception e)
        {
            Debug.LogError($"Unity Services initialization failed: {e}");
        }
    }

    private void OnLevelPlayInitSuccess(LevelPlayConfiguration configuration)
    {
        if (isLevelPlayInitialized)
            return;

        isLevelPlayInitialized = true;
        CreateAndLoadAds();
    }

    private void OnLevelPlayInitFailed(LevelPlayInitError error)
    {
        Debug.LogError($"LevelPlay initialization failed: {error}");
    }

    private void CreateAndLoadAds()
    {
        interstitialAd = new LevelPlayInterstitialAd(IS_adUnitId);
        rewardedAd = new LevelPlayRewardedAd(RW_adUnitId);
        LevelPlayBannerAd.Config bannerConfig = ConfigBanner();

        bannerAd = new LevelPlayBannerAd(Banner_adUnitId, bannerConfig);

        interstitialAd.OnAdClosed += OnAdClosed;
        interstitialAd.OnAdDisplayFailed += OnAdFailedShow;
        interstitialAd.OnAdLoaded += OnAdLoaded;
        interstitialAd.OnAdLoadFailed += OnAdFailedLoad;
        interstitialAd.OnAdClicked += OnAdClicked;

        rewardedAd.OnAdClosed += OnRewardedClosed;
        rewardedAd.OnAdDisplayFailed += OnRewardedFailedShow;
        rewardedAd.OnAdLoaded += OnRewardedLoaded;
        rewardedAd.OnAdLoadFailed += OnRewardedFailedLoad;
        rewardedAd.OnAdClicked += OnRewardedClicked;
        rewardedAd.OnAdRewarded += OnRewardedAdRewarded;

        bannerAd.OnAdClicked += OnBannerClicked;
        bannerAd.OnAdLoaded += OnBannerAdLoaded;
        bannerAd.OnAdLoadFailed += OnBannerAdFailedLoad;

        interstitialAd.LoadAd();
        rewardedAd.LoadAd();
        bannerAd.LoadAd();
    }

    private LevelPlayBannerAd.Config ConfigBanner()
    {
        return new LevelPlayBannerAd.Config.Builder()
            .SetSize(LevelPlayAdSize.CreateCustomBannerSize(Screen.width, bannerHeight))
            .SetPosition(LevelPlayBannerPosition.BottomCenter)
            .SetDisplayOnLoad(true)
            .Build();
    }

    private void OnBannerClicked(LevelPlayAdInfo adInfo)
    {
        Debug.Log("Banner clicked");
        onBannerClickedEvent.Invoke(this, EventArgs.Empty);
    }
    private void OnBannerAdLoaded(LevelPlayAdInfo adInfo)
    {
        Debug.Log("Banner Ad loaded");
        EventManager.TriggerEvent(GameEvent.BANNER_AD_LOADED);
    }
    private void OnBannerAdFailedLoad(LevelPlayAdError error)
    {
        Debug.LogError($"Banner Ad failed to load: {error.ErrorMessage}");
    }

    void Update()
    {
        timeSinceLastAd += Time.deltaTime;

    }

    public void TryShowAd()
    {
        if (timeSinceLastAd >= adInterval && levelsSinceLastAd > levelInterval && interstitialAd != null && interstitialAd.IsAdReady())
        {
            ShowAd();
            timeSinceLastAd = 0.0f;
            levelsSinceLastAd = 0;
        }
    }

    public void ShowAd()
    {
        if (interstitialAd != null && interstitialAd.IsAdReady())
        {
            interstitialAd.ShowAd();
            EventManager.TriggerEvent(GameEvent.AD_SHOWN);
        }
        else
        {
            Debug.Log("Advertisement not ready");
        }
    }
    public void ShowRewardedAd()
    {
        if (rewardedAd != null && rewardedAd.IsAdReady())
        {
            rewardedGrantedInCurrentShow = false;
            rewardedAd.ShowAd();
            EventManager.TriggerEvent(GameEvent.REWARDED_AD_SHOWN);
        }
        else
        {
            Debug.Log("Rewarded Ad not ready");
        }
    }

    private void OnAdClosed(LevelPlayAdInfo adInfo)
    {
        Debug.Log("Ad closed");
        interstitialAd.LoadAd();
        onAdClosedEvent.Invoke(this, EventArgs.Empty);
        EventManager.TriggerEvent(GameEvent.AD_CLOSED);
    }

    private void OnAdFailedShow(LevelPlayAdInfo adInfo, LevelPlayAdError error)
    {
        Debug.LogError($"Ad failed to show: {error.ErrorMessage}");
        interstitialAd.LoadAd();
        onAdFailedShowEvent.Invoke(this, error);
        EventManager.TriggerEvent(GameEvent.AD_FAILED, new EventParam(paramStr: error.ErrorMessage));
    }

    private void OnAdLoaded(LevelPlayAdInfo adInfo)
    {
        Debug.Log("Ad loaded");
        onAdLoadedEvent.Invoke(this, EventArgs.Empty);
    }

    private void OnAdFailedLoad(LevelPlayAdError error)
    {
        Debug.LogError($"Ad failed to load: {error.ErrorMessage}");
        onAdFailedLoadEvent.Invoke(this, error);
    }

    private void OnRewardedClosed(LevelPlayAdInfo adInfo)
    {
        Debug.Log("Rewarded Ad closed");
        rewardedAd.LoadAd();
        onRewardedClosedEvent.Invoke(this, EventArgs.Empty);

        if (rewardedGrantedInCurrentShow)
        {
            EventManager.TriggerEvent(GameEvent.REWARDED_AD_COMPLETED);
        }
    }

    private void OnRewardedFailedShow(LevelPlayAdInfo adInfo, LevelPlayAdError error)
    {
        Debug.LogError($"Rewarded Ad failed to show: {error.ErrorMessage}");
        rewardedAd.LoadAd();
        onRewardedFailedShowEvent.Invoke(this, error);
        EventManager.TriggerEvent(GameEvent.REWARDED_AD_FAILED, new EventParam(paramStr: error.ErrorMessage));
    }

    private void OnRewardedLoaded(LevelPlayAdInfo adInfo)
    {
        Debug.Log("Rewarded Ad loaded");
        onRewardedLoadedEvent.Invoke(this, EventArgs.Empty);
    }

    private void OnRewardedFailedLoad(LevelPlayAdError error)
    {
        Debug.LogError($"Rewarded Ad failed to load: {error.ErrorMessage}");
        onRewardedFailedLoadEvent.Invoke(this, error);
    }

    private void OnAdClicked(LevelPlayAdInfo adInfo)
    {
        Debug.Log("Ad clicked");
        onAdClickedEvent.Invoke(this, EventArgs.Empty);
    }

    private void OnRewardedClicked(LevelPlayAdInfo adInfo)
    {
        Debug.Log("Rewarded Ad clicked");
        onRewardedClickedEvent.Invoke(this, EventArgs.Empty);
    }

    private void OnRewardedAdRewarded(LevelPlayAdInfo adInfo, LevelPlayReward reward)
    {
        rewardedGrantedInCurrentShow = true;
    }


    private void OnDestroy()
    {
        LevelPlay.OnInitSuccess -= OnLevelPlayInitSuccess;
        LevelPlay.OnInitFailed -= OnLevelPlayInitFailed;

        if (interstitialAd != null)
        {
            interstitialAd.OnAdClosed -= OnAdClosed;
            interstitialAd.OnAdDisplayFailed -= OnAdFailedShow;
            interstitialAd.OnAdLoaded -= OnAdLoaded;
            interstitialAd.OnAdLoadFailed -= OnAdFailedLoad;
            interstitialAd.OnAdClicked -= OnAdClicked;
            interstitialAd.DestroyAd();
        }

        if (rewardedAd != null)
        {
            rewardedAd.OnAdClosed -= OnRewardedClosed;
            rewardedAd.OnAdDisplayFailed -= OnRewardedFailedShow;
            rewardedAd.OnAdLoaded -= OnRewardedLoaded;
            rewardedAd.OnAdLoadFailed -= OnRewardedFailedLoad;
            rewardedAd.OnAdClicked -= OnRewardedClicked;
            rewardedAd.OnAdRewarded -= OnRewardedAdRewarded;
            rewardedAd.DestroyAd();
        }

        if (bannerAd != null)
        {
            bannerAd.OnAdClicked -= OnBannerClicked;
            bannerAd.DestroyAd();
        }
    }
}
