using System.Collections.Generic;
using Unity.Services.Core;
using UnityEngine.UnityConsent ;
using UnityEngine;


namespace Game
{
    public class AnalyticsManager : MonoBehaviour
    {
        public static AnalyticsManager Instance { get; private set; }

        private IAnalyticsProvider _provider;

        private async void Awake()
        {
            await UnityServices.InitializeAsync();
            EndUserConsent.SetConsentState(new ConsentState
            {
                AnalyticsIntent = ConsentStatus.Granted,
                AdsIntent = ConsentStatus.Granted
            });
            
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
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

        public static void SendEvent(string eventName)
        {
            Instance._provider.SendEvent(eventName);
        }

        public static void SendEvent(string eventName, Dictionary<string, object> parameters)
        {
            Instance._provider.SendEvent(eventName, parameters);
        }
    }

}