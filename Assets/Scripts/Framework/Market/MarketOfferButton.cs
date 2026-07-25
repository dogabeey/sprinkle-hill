using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game
{
    public sealed class MarketOfferButton : MonoBehaviour
    {
        [Tooltip("Invokes the configured IBuyable purchase when the player can afford this offer.")]
        [SerializeField] private Button button;
        [SerializeField] private Image offerIcon;
        [Tooltip("Displays the BuyBundle quantity.")]
        [SerializeField] private TMP_Text amountText;
        [Tooltip("Displays the discounted total price. It is hidden for ad-based offers.")]
        [SerializeField] private TMP_Text priceText;
        [Tooltip("Optional visual displayed instead of a price when Buy With Ad is enabled.")]
        [SerializeField] private GameObject adIndicator;

        private IBuyable listing;
        private IBuyable.BuyBundle bundle;
        private GameObject source;

        public void Bind(IBuyable buyable, IBuyable.BuyBundle buyBundle, GameObject purchaseSource, Sprite productIcon)
        {
            listing = buyable;
            bundle = buyBundle;
            source = purchaseSource;
            if (amountText != null) amountText.text = $"x{bundle.buyCount}";
            if (offerIcon != null) offerIcon.sprite = productIcon;
            if (priceText != null) priceText.text = bundle.buyWithAd ? string.Empty : bundle.GetTotalCost(listing.GetCost()).ToString();
            if (adIndicator != null) adIndicator.SetActive(bundle.buyWithAd);
            if (button != null)
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(Buy);
            }
            RefreshState();
        }

        public void RefreshState()
        {
            if (button == null || listing == null || bundle == null) return;
            button.interactable = bundle.buyWithAd || (listing.CostCurrency != null && CurrencyManager.Instance != null && CurrencyManager.Instance.GetCurrencyAmount(listing.CostCurrency) >= bundle.GetTotalCost(listing.GetCost()));
        }

        private void Buy()
        {
            if (listing.TryBuy(bundle, source))
                RefreshState();
        }
    }
}
