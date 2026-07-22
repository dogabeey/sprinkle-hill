using System;
using System.Linq;
using UnityEngine;
using Game.EventManagement;

namespace Game
{
    /// <summary>Extensible reward contract used by MarketProduct. Add a derived reward for new inventory systems.</summary>
    [Serializable]
    public abstract class MarketReward
    {
        public virtual bool CanGrant() => true;
        public abstract void Grant(int bundleQuantity, GameObject source);
    }

    [Serializable]
    public sealed class CurrencyMarketReward : MarketReward
    {
        [Tooltip("Currency granted by this reward.")]
        [SerializeField] private CurrencyModel currency;

        public override bool CanGrant() => currency != null && CurrencyManager.Instance != null;
        public override void Grant(int bundleQuantity, GameObject source) => CurrencyManager.Instance.AddCurrency(currency, bundleQuantity, source);
    }

    [Serializable]
    public sealed class ActionInventoryMarketReward : MarketReward
    {
        [Tooltip("Exact ItemName of an ActionBarItem to add to the player's inventory. This supports boosters and pre-level power-ups.")]
        [SerializeField] private string actionItemName;

        public override bool CanGrant() => FindAction() != null;

        public override void Grant(int bundleQuantity, GameObject source)
        {
            ActionBarItem action = FindAction();
            if (action == null) return;
            action.currentCount += bundleQuantity;
            ActionBarManager.Instance.GetActionBarView(action)?.DrawUI();
        }

        private ActionBarItem FindAction()
        {
            return ActionBarManager.Instance == null ? null : ActionBarManager.Instance.actionBarItemList.FirstOrDefault(item => item != null && item.ItemName == actionItemName);
        }
    }

    [Serializable]
    public sealed class EventMarketReward : MarketReward
    {
        [Tooltip("Event raised after purchase. Use this for custom systems, unlocks, or any reward handled by an event listener.")]
        [SerializeField] private GameEvent eventToTrigger = GameEvent.NONE;
        [Tooltip("Optional identifier passed through EventParam.paramStr so listeners can distinguish products.")]
        [SerializeField] private string rewardId;

        public override void Grant(int bundleQuantity, GameObject source)
        {
            if (eventToTrigger == GameEvent.NONE) return;
            EventManager.TriggerEvent(eventToTrigger, new EventParam(paramObj: source, paramInt: bundleQuantity, paramStr: rewardId));
        }
    }
}
