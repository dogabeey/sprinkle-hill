using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine; using Game.EventManagement;
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
            GameObject objectSource = null;
            if (buyBundle?.buyableReference is ActionBarItem actionBarItem && GameManager.Instance != null && GameManager.Instance.actionBarManager != null)
            {
                ActionBarView matchingActionBarView = GameManager.Instance.actionBarManager.GetActionBarView(actionBarItem);
                if (matchingActionBarView != null)
                    objectSource = matchingActionBarView.gameObject;
            }

            if (buyBundle != null && buyBundle.buyableReference != null && buyBundle.buyableReference.TryBuy(buyBundle, objectSource))
            {
                AnalyticsManager.SendEvent(new BoosterBoughtEvent
                {
                    LevelIndex = GameManager.Instance.CurrentLevelIndex,
                    BoosterName = buyBundle.buyableReference.ItemName,
                    CashAmount = (int) CurrencyManager.Instance.GetCurrencyAmount(buyBundle.buyableReference.CostCurrency),
                    ItemAmount = buyBundle.buyCount,
                });
                ScreenManager.Instance.CloseAllScreens();
            }
        }
    }
}

