using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game
{
    public class  BuyScreenNode : MonoBehaviour
    {
        public Image itemImage;
        public TMP_Text itemCountText;
        public TMP_Text costText;
        public Button buyButton;
        [Header("Settings")]
        public string costTextFormat = "<sprite index={0}>{1}";
        public string itemCountTextFormat = "x{0}";


        internal IBuyable referenceBuyable;
        internal int buyAmount;

        /// <summary>
        /// Evaluates and returns the appropriate sprite based on the current state or configuration.
        /// </summary>
        /// <returns>A Sprite object representing the evaluated result. The returned value may be null if no suitable sprite is
        /// determined.</returns>
        public Sprite EvaluateSprite(IBuyable buyable, int count)
        {
            List<IBuyable.BuyBundle> buyspriteConfig = buyable.BuyConfig;

            if (buyspriteConfig == null || buyspriteConfig.Count == 0)
                return null;
            // Sort the configuration by count in descending order to find the highest applicable sprite
            buyspriteConfig.Sort((a, b) => b.buyCount.CompareTo(a.buyCount));
            foreach (var config in buyspriteConfig)
            {
                if (count >= config.buyCount)
                {
                    return config.BuySprite;
                }
            }
            return null; // Return null if no applicable sprite is found
        }

        public void Init(IBuyable buyable, int count)
        {
            referenceBuyable = buyable;
            buyAmount = count;

            if (itemImage) itemImage.sprite = EvaluateSprite(buyable, count);
            if (itemCountText) itemCountText.text = string.Format(itemCountTextFormat, count);
            if (costText) costText.text = string.Format(costTextFormat, buyable.CostCurrency.spriteIndexForUI, buyable.GetCost() * count);
            buyButton.onClick.RemoveAllListeners();
            buyButton.onClick.AddListener(() =>
            {
                OnBuyButtonClicked();
            });
        }

        private void OnBuyButtonClicked()
        {
            if (referenceBuyable != null)
            {
                referenceBuyable.Buy(buyAmount);
            }
        }
    }
}

