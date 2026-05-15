using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game
{
    public interface IBuyable
    {
        public int GetCost();
        bool TryBuy(BuyBundle buyBundle, GameObject source = null);

        public string ItemName { get; }
        public string ItemDescription { get; }
        public CurrencyModel CostCurrency { get; }
        public List<BuyBundle> BuyConfig { get; }

        [Serializable]
        public class BuyBundle
        {
            internal IBuyable buyableReference;

            public int buyCount; // The count at which the buy sprite will change to the specified sprite. If the count is greater than or equal to the count threshold, the buy sprite will be used. This is useful for assigning different sprites when you buy in greater bundles.
            public float discountPercentage; // The discount percentage to be applied when the count threshold is reached. For example, if the discount percentage is 0.1 (10%), and the original cost is 100, the cost will be reduced to 90 when the count threshold is reached. This is useful for encouraging players to buy in greater bundles by offering them a discount.
            public Sprite buySprite; // The sprite to be used when the count is greater than or equal to the count threshold.

            public BuyBundle(IBuyable buyableReference, int buyCount, float discountPercentage, Sprite buySprite)
            {
                this.buyableReference = buyableReference;
                this.buyCount = buyCount;
                this.discountPercentage = discountPercentage;
                this.buySprite = buySprite;
            }

            public int GetTotalCost(int baseCost)
            {
                return Mathf.RoundToInt((baseCost * buyCount) * (1 - discountPercentage));
            }
        }
    }
}
