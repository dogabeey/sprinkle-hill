using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Game
{
    [CreateAssetMenu(fileName = "Market Product", menuName = "Game/Market/Product")]
    public sealed class MarketProduct : ScriptableObject, IBuyable
    {
        [Header("Display")]
        [Tooltip("Player-facing product name displayed in the market listing.")]
        [SerializeField] private string itemName;
        [Tooltip("Player-facing explanation displayed beneath the product name.")]
        [TextArea, SerializeField] private string itemDescription;
        [Tooltip("Determines which MarketCategoryContainer displays this product.")]
        [SerializeField] private MarketCategory itemCategory = MarketCategory.Currency;
        [Header("Price")]
        [Tooltip("Currency the player spends to buy this product.")]
        [SerializeField] private CurrencyModel costCurrency;
        [Tooltip("Base price for one item before a BuyBundle quantity or discount is applied.")]
        [Min(0), SerializeField] private int cost;
        [Tooltip("Purchasing options shown on this product. Buy Count is the single reward quantity: every configured reward is granted that many times.")]
        [SerializeField] private List<IBuyable.BuyBundle> buyConfig = new List<IBuyable.BuyBundle>();
        [Header("Reward")]
        [Tooltip("Rewards granted after a successful purchase. Every reward uses the selected Buy Count; use CurrencyMarketReward, ActionInventoryMarketReward, EventMarketReward, or add a custom MarketReward type.")]
        [SerializeReference] private List<MarketReward> rewards = new List<MarketReward>();

        public string ItemName => itemName;
        public string ItemDescription => itemDescription;
        public MarketCategory ItemCategory => itemCategory;
        public CurrencyModel CostCurrency => costCurrency;
        public List<IBuyable.BuyBundle> BuyConfig => buyConfig;
        public int GetCost() => cost;

        public bool TryBuy(IBuyable.BuyBundle bundle, GameObject source = null)
        {
            if (bundle == null || bundle.buyWithAd || CostCurrency == null || CurrencyManager.Instance == null) return false;
            int totalCost = bundle.GetTotalCost(cost);
            if (CurrencyManager.Instance.GetCurrencyAmount(CostCurrency) < totalCost) return false;
            if (rewards == null || rewards.Count == 0 || rewards.Any(reward => reward == null || !reward.CanGrant())) return false;
            CurrencyManager.Instance.AddCurrency(CostCurrency, -totalCost);
            foreach (MarketReward reward in rewards)
                reward.Grant(bundle.buyCount, source);
            return true;
        }
    }
}
