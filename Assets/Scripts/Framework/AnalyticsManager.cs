using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Analytics;


namespace Game
{
    public interface IAnalyticsProvider
    {
        void Initialize();
        void SendEvent(string eventName);
        void SendEvent(string eventName, Dictionary<string, object> parameters);
    }
    public class AnalyticsManager : MonoBehaviour
    {
        public static AnalyticsManager Instance { get; private set; }

        private IAnalyticsProvider _provider;

        private void Awake()
        {
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

        public void SendEvent(string eventName)
        {
            _provider.SendEvent(eventName);
        }

        public void SendEvent(string eventName, Dictionary<string, object> parameters)
        {
            _provider.SendEvent(eventName, parameters);
        }
    }

    public class AnalyticsProvider : IAnalyticsProvider
    {
        public void Initialize()
        {
            // Initialize any analytics SDKs here if needed
        }
        public void SendEvent(string eventName)
        {
            Analytics.CustomEvent(eventName);
        }
        public void SendEvent(string eventName, Dictionary<string, object> parameters)
        {
            Analytics.CustomEvent(eventName, parameters);
        }
    }

}