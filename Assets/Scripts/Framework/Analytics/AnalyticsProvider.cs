using System.Collections.Generic;
using UnityEngine;

using UnityEngine.Analytics;
using Unity.Services.Core;
using Unity.Services.Analytics;

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
        public void SendEvent<T>(T analyticsEvent) where T : Unity.Services.Analytics.Event
        {
            AnalyticsService.Instance.RecordEvent(analyticsEvent);
        }

    }
}