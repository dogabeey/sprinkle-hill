using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game
{
    public class  BuyScreenNode : MonoBehaviour
    {
        public Image itemImage;
        public TMP_Text itemNameText;
        public TMP_Text itemDescriptionText;
        public TMP_Text costText;
        public Button buyButton;


        internal IBuyable referenceBuyable;
        internal int buyAmount;

        /// <summary>
        /// Evaluates and returns the appropriate sprite based on the current state or configuration.
        /// </summary>
        /// <returns>A Sprite object representing the evaluated result. The returned value may be null if no suitable sprite is
        /// determined.</returns>
        public Sprite EvaluateSprite(IBuyable buyable, int count)
        {
            List<IBuyable.BuySpriteAtCount> buyspriteConfig = buyable.BuySprites;

            if (buyspriteConfig == null || buyspriteConfig.Count == 0)
                return null;
            // Sort the configuration by count in descending order to find the highest applicable sprite
            buyspriteConfig.Sort((a, b) => b.countThreshold.CompareTo(a.countThreshold));
            foreach (var config in buyspriteConfig)
            {
                if (count >= config.countThreshold)
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
            if (itemNameText) itemNameText.text = buyable.ActionName;
            if (itemDescriptionText) itemDescriptionText.text = buyable.ActionDescription;
            if (costText) costText.text = $"<sprite index={buyable.CostCurrency.spriteIndexForUI}>{buyable.GetCost() * count}";
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

