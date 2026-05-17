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
        public void SendEvent(string eventName)
        {
            Debug.Log($"Analytics Event Sent: {eventName}");
#if UNITY_ANALYTICS
            Analytics.CustomEvent(eventName);
#endif
        }
        public void SendEvent<T>(T analyticsEvent) where T : Unity.Services.Analytics.Event
        {
            AnalyticsService.Instance.RecordEvent(analyticsEvent);
        }

    }

    // Level Win Event
    public class LevelWinEvent : Unity.Services.Analytics.Event
    {
        public int LevelIndex { set { SetParameter("levelIndex", value); } }
        public int Moves { set { SetParameter("moves", value); } }


        public LevelWinEvent() : base("levelWin")
        {

        }
        public LevelWinEvent(int levelIndex, int moves) : base("levelWin")
        {
            LevelIndex = levelIndex;
            Moves = moves;
        }
    }

    // Level Fail Event
    public class LevelFailedEvent : Unity.Services.Analytics.Event
    {
        public int LevelIndex { set { SetParameter("levelIndex", value); } }

        public LevelFailedEvent() : base("levelFail")
        {
        }
        public LevelFailedEvent(int levelIndex) : base("levelFail")
        {
            LevelIndex = levelIndex;
        }
    }

    // Booster Bought Event
    public class BoosterBoughtEvent : Unity.Services.Analytics.Event
    {
        public int LevelIndex { set { SetParameter("levelIndex", value); } }
        public string BoosterName { set { SetParameter("boosterName", value); } }
        public int CashAmount { set { SetParameter("cashAmount", value); } }
        public int ItemAmount { set { SetParameter("itemAmount", value); } }
        public BoosterBoughtEvent() : base("boosterBought")
        {
        }
        public BoosterBoughtEvent(int levelIndex, string boosterName, int cashAmount, int itemAmount) : base("boosterBought")
        {
            LevelIndex = levelIndex;
            BoosterName = boosterName;
            CashAmount = cashAmount;
            ItemAmount = itemAmount;
        }
    }

    // Moves Bought Event
    public class ExtraMovesOrTimeBought : Unity.Services.Analytics.Event
    {
        public int LevelIndex { set { SetParameter("levelIndex", value); } }
        public int CashAmount { set { SetParameter("cashAmount", value); } }
        public int ExtraMoves { set { SetParameter("extraMoves", value); } }
        public ExtraMovesOrTimeBought() : base("movesBought")
        {
        }
        public ExtraMovesOrTimeBought(int levelIndex, int cashAmount, int extraMoves) : base("movesBought")
        {
            LevelIndex = levelIndex;
            CashAmount = cashAmount;
            ExtraMoves = extraMoves;
        }
    }
}