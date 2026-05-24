using System.Collections.Generic;
using Unity.Services.Core;
using UnityEngine.UnityConsent ;
using UnityEngine; using Game.EventManagement;
using System;
using Game.SimpleJSON;
using Game.EventManagement;


namespace Game
{
    public class AnalyticsManager : MonoBehaviour, ISaveable
    {
        public static AnalyticsManager Instance { get; private set; }

        public ConsentState currentConsentState;

        public string SaveId => "analytics";
        public SaveDataType SaveDataType => SaveDataType.MetaData;

        private IAnalyticsProvider _provider;

        private async void Awake()
        {
            SaveManager.Instance.Register(this);
            await UnityServices.InitializeAsync();
            
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            if (!Load())
            {
                ScreenManager.Instance.Show(Screens.ConsentPopup);
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            Initialize();
        }

        private void Initialize()
        {
            // Swap provider here later if needed
            _provider = new AnalyticsProvider();

            _provider.Initialize();
        }

        public static void SendEvent(Unity.Services.Analytics.Event @event)
        {
            Instance._provider.SendEvent(@event);
        }

        public Dictionary<string, object> Save()
        {
            Dictionary<string, object> json = new Dictionary<string, object>();

            json["currentConsentState"] = new Dictionary<string, object>
            {
                {"AnalyticsIntent", currentConsentState.AnalyticsIntent.ToString()},
                {"AdsIntent", currentConsentState.AdsIntent.ToString()}
            };

            return json;
        }

        public bool Load(Action onLoadSuccess = null, Action onLoadFail = null)
        {
            JSONNode json = GameManager.Instance.saveManager.LoadSave(this);

            if (json == null)
            {
                onLoadFail?.Invoke();
                return false;
            }

            currentConsentState = new ConsentState
            {
                AnalyticsIntent = Enum.Parse<ConsentStatus>(json["currentConsentState"]["AnalyticsIntent"]),
                AdsIntent = Enum.Parse<ConsentStatus>(json["currentConsentState"]["AdsIntent"])
            };

            onLoadSuccess?.Invoke();
            return true;
        }
    }

}