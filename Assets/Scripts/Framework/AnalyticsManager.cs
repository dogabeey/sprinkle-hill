using System.Collections.Generic;
using UnityEngine;


namespace Game
{
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