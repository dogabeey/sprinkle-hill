using System.Collections.Generic;
using UnityEngine;

#if UNITY_ANALYTICS
using UnityEngine.Analytics;
#endif

namespace Game
{
    public class AnalyticsProvider : IAnalyticsProvider
    {
        public AnalyticsProvider()
        {
        }

        public void Initialize()
        {
            // Initialize any analytics SDKs here if needed
            
        }
        public void SendEvent(string eventName)
        {
            Debug.Log($"Analytics Event Sent: {eventName}");
#if UNITY_ANALYTICS
            Analytics.CustomEvent(eventName);
#endif
        }
        public void SendEvent(string eventName, Dictionary<string, object> parameters)
        {
            Debug.Log($"Analytics Event Sent: {eventName} with parameters: {string.Join(", ", parameters)}");
#if UNITY_ANALYTICS
            Analytics.CustomEvent(eventName, parameters);
#endif
        }

    }
}