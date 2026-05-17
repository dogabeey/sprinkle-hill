using System.Collections.Generic;


namespace Game
{
    public interface IAnalyticsProvider
    {
        void Initialize();
        void SendEvent(string eventName);
        void SendEvent(string eventName, Dictionary<string, object> parameters);
    }

}