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


        internal IBuyable.BuyBundle buyBundle;
        internal int buyAmount;


        public void Init(IBuyable.BuyBundle buyBundle) 
        {
            this.buyBundle = buyBundle;
            buyAmount = buyBundle.buyCount;
            if (itemImage) itemImage.sprite = buyBundle?.buySprite;
            if (itemCountText) itemCountText.text = string.Format(itemCountTextFormat, buyAmount);
            if (costText) costText.text = string.Format(costTextFormat, buyBundle.buyableReference.CostCurrency.spriteIndexForUI, buyBundle?.GetTotalCost(buyBundle.buyableReference.GetCost()) ?? 0);
            buyButton.interactable = buyBundle != null && buyBundle.buyableReference != null && buyBundle.GetTotalCost(buyBundle.buyableReference.GetCost()) <= CurrencyManager.Instance.GetCurrencyAmount(buyBundle.buyableReference.CostCurrency);
            buyButton.onClick.RemoveAllListeners();
            buyButton.onClick.AddListener(() =>
            {
                OnBuyButtonClicked(buyBundle);
            });
        }

        private void OnBuyButtonClicked(IBuyable.BuyBundle buyBundle)
        {
            if (buyBundle != null && buyBundle.buyableReference != null && buyBundle.buyableReference.TryBuy(buyBundle))
            {
                ScreenManager.Instance.CloseAllScreens();
            }
        }
    }
}

