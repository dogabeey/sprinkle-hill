using System.Collections.Generic;
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
#if UNITY_ANALYTICS
            Analytics.CustomEvent(eventName);
#endif
        }
        public void SendEvent(string eventName, Dictionary<string, object> parameters)
        {
#if UNITY_ANALYTICS
            Analytics.CustomEvent(eventName, parameters);
#endif
        }
    }

}