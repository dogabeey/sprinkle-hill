using System.Collections.Generic;


namespace Game
{
    public interface IAnalyticsProvider
    {
        void Initialize();
        void SendEvent<T>(T analyticsEvent) where T : Unity.Services.Analytics.Event;
    }

}