using System.Collections.Generic;
using System.Linq;
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


        public void Init(IBuyable buyable, int count)
        {
            referenceBuyable = buyable;
            buyAmount = count;
            var buyBundle = buyable.BuyConfig.FirstOrDefault(b => b.buyCount == count);

            if (itemImage) itemImage.sprite = buyBundle?.BuySprite;
            if (itemCountText) itemCountText.text = string.Format(itemCountTextFormat, count);
            if (costText) costText.text = string.Format(costTextFormat, buyable.CostCurrency.spriteIndexForUI, buyBundle?.GetTotalCost(buyable.GetCost()) ?? 0);
            buyButton.onClick.RemoveAllListeners();
            buyButton.onClick.AddListener(() =>
            {
                OnBuyButtonClicked(buyBundle?.GetTotalCost(buyable.GetCost()) ?? 0, count);
            });
        }

        private void OnBuyButtonClicked(int cost, int count)
        {
            if (referenceBuyable != null)
            {
                referenceBuyable.Buy(cost, count);
            }
        }
    }
}

