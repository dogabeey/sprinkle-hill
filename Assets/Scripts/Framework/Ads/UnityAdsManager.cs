using System;
using UnityEngine;
using Unity.Services.Core;
using Unity.Services.Mediation;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using Game;

public class UnityAdsManager : SingletonComponent<UnityAdsManager>
{
    public float adInterval = 300.0f; // Time interval between ads in seconds
    public int levelInterval = 2; // Time interval between ads in seconds

    internal UnityEvent<object, EventArgs> onAdClosedEvent = new();
    internal UnityEvent<object, ShowErrorEventArgs> onAdFailedShowEvent = new();
    internal UnityEvent<object, EventArgs> onAdLoadedEvent = new();
    internal UnityEvent<object, LoadErrorEventArgs> onAdFailedLoadEvent = new();
    internal UnityEvent<object, EventArgs> onAdClickedEvent = new();
    internal UnityEvent<object, EventArgs> onRewardedClosedEvent = new();
    internal UnityEvent<object, ShowErrorEventArgs> onRewardedFailedShowEvent = new();
    internal UnityEvent<object, EventArgs> onRewardedLoadedEvent = new();
    internal UnityEvent<object, LoadErrorEventArgs> onRewardedFailedLoadEvent = new();
    internal UnityEvent<object, EventArgs> onRewardedClickedEvent = new();
    internal UnityEvent<object, EventArgs> onBannerClickedEvent = new();


    private string gameId;
    private string IS_adUnitId;
    private string RW_adUnitId;
    private string Banner_adUnitId;
    private float timeSinceLastAd;
    private IInterstitialAd interstitialAd;
    private IRewardedAd rewardedAd;
    private IBannerAd bannerAd;
    private int levelsSinceLastAd = 0;

    private void OnEnable()
    {
        EventManager.StartListening(ConstantManager.GameEvents.LEVEL_STARTED, OnLevelStarted);
    }
    private void OnDisable()
    {
        EventManager.StopListening(ConstantManager.GameEvents.LEVEL_STARTED, OnLevelStarted);
    }
    private void OnLevelStarted(EventParam e)
    {
        levelsSinceLastAd++;
    }

    async void Start()
    {
        timeSinceLastAd = 0.0f;

#if UNITY_ANDROID
        gameId = "5622038";
        IS_adUnitId = "Interstitial_Android";
        RW_adUnitId = "Rewarded_Android";
        Banner_adUnitId = "Banner_Android";
#elif UNITY_IOS
        gameId = "5622039"; // Replace with your actual iOS Game ID
        IS_adUnitId = "Interstitial_iOS"; // Replace with your actual iOS Ad Unit ID
        RW_adUnitId = "Rewarded_iOS"; // Replace with your actual iOS Ad Unit ID
        Banner_adUnitId = "Banner_iOS"; // Replace with your actual iOS Ad Unit ID
#else
        Debug.LogError("Unsupported platform");
        return;
#endif

        try
        {
            // Initialize the Unity Services core
            await UnityServices.InitializeAsync();

            // Create an instance of the interstitial ad
            interstitialAd = MediationService.Instance.CreateInterstitialAd(IS_adUnitId);
            rewardedAd = MediationService.Instance.CreateRewardedAd(RW_adUnitId);
            bannerAd = MediationService.Instance.CreateBannerAd(Banner_adUnitId, new BannerAdSize(BannerAdPredefinedSize.Banner), BannerAdAnchor.BottomCenter);

            // Subscribe to events
            interstitialAd.OnClosed += OnAdClosed;
            interstitialAd.OnFailedShow += OnAdFailedShow;
            interstitialAd.OnLoaded += OnAdLoaded;
            interstitialAd.OnFailedLoad += OnAdFailedLoad;
            interstitialAd.OnClicked += OnAdClicked;

            rewardedAd.OnClosed += OnRewardedClosed;
            rewardedAd.OnFailedShow += OnRewardedFailedShow;
            rewardedAd.OnLoaded += OnRewardedLoaded;
            rewardedAd.OnFailedLoad += OnRewardedFailedLoad;
            rewardedAd.OnClicked += OnRewardedClicked;

            bannerAd.OnClicked += OnBannerClicked;

            // Load the ad
            await interstitialAd.LoadAsync();
            await rewardedAd.LoadAsync();
            await bannerAd.LoadAsync();
        }
        catch (Exception e)
        {
            Debug.LogError($"Unity Services initialization failed: {e}");
        }
    }

    private void OnBannerClicked(object sender, EventArgs e)
    {
        Debug.Log("Banner clicked");
    }

    void Update()
    {
        timeSinceLastAd += Time.deltaTime;

    }

    public void TryShowAd()
    {
        if (timeSinceLastAd >= adInterval && levelsSinceLastAd > levelInterval && interstitialAd.AdState == AdState.Loaded)
        {
            ShowAd();
            timeSinceLastAd = 0.0f;
            levelsSinceLastAd = 0;
        }
    }

    public void ShowAd()
    {
        if (interstitialAd.AdState == AdState.Loaded)
        {
            interstitialAd.ShowAsync();
        }
        else
        {
            Debug.Log("Advertisement not ready");
        }
    }
    public void ShowRewardedAd()
    {
        if (rewardedAd.AdState == AdState.Loaded)
        {
            rewardedAd.ShowAsync();
        }
        else
        {
            Debug.Log("Rewarded Ad not ready");
        }
    }

    private void OnAdClosed(object sender, EventArgs e)
    {
        Debug.Log("Ad closed");
        interstitialAd.LoadAsync();
        onAdClickedEvent.Invoke(sender, e);
    }

    private void OnAdFailedShow(object sender, ShowErrorEventArgs e)
    {
        Debug.LogError($"Ad failed to show: {e.Message}");
        interstitialAd.LoadAsync();
        onAdFailedShowEvent.Invoke(sender, e);
    }

    private void OnAdLoaded(object sender, EventArgs e)
    {
        Debug.Log("Ad loaded");
        interstitialAd.LoadAsync();
        onAdLoadedEvent.Invoke(sender, e);
    }

    private void OnAdFailedLoad(object sender, LoadErrorEventArgs e)
    {
        Debug.LogError($"Ad failed to load: {e.Message}");
        onAdFailedLoadEvent.Invoke(sender, e);
    }
    private void OnRewardedClosed(object sender, EventArgs e)
    {
        Debug.Log("Rewarded Ad closed");
        rewardedAd.LoadAsync();
        onRewardedClosedEvent.Invoke(sender, e);
    }
    private void OnRewardedFailedShow(object sender, ShowErrorEventArgs e)
    {
        Debug.LogError($"Rewarded Ad failed to show: {e.Message}");
        rewardedAd.LoadAsync();
        onRewardedFailedShowEvent.Invoke(sender, e);
    }
    private void OnRewardedLoaded(object sender, EventArgs e)
    {
        Debug.Log("Rewarded Ad loaded");
        rewardedAd.LoadAsync();
        onRewardedLoadedEvent.Invoke(sender, e);
    }
    private void OnRewardedFailedLoad(object sender, LoadErrorEventArgs e)
    {
        Debug.LogError($"Rewarded Ad failed to load: {e.Message}");
        onRewardedFailedLoadEvent.Invoke(sender, e);
    }
    private void OnAdClicked(object sender, EventArgs e)
    {
        Debug.Log("Ad clicked");
        onAdClickedEvent.Invoke(sender, e);
    }
    private void OnRewardedClicked(object sender, EventArgs e)
    {
        Debug.Log("Rewarded Ad clicked");
        onRewardedClickedEvent.Invoke(sender, e);
    }


    private void OnDestroy()
    {
        // Clean up events
        if (interstitialAd != null)
        {
            interstitialAd.OnClosed -= OnAdClosed;
            interstitialAd.OnFailedShow -= OnAdFailedShow;
            interstitialAd.OnLoaded -= OnAdLoaded;
            interstitialAd.OnFailedLoad -= OnAdFailedLoad;
        }
    }

}
