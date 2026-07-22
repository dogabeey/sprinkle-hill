using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game
{
    /// <summary>Reusable, fully assignable prefab view for one market product.</summary>
    public sealed class MarketListingView : MonoBehaviour
    {
        [Header("Prefab References")]
        [Tooltip("Optional product icon. The first configured bundle sprite is used by default.")]
        [SerializeField] private Image icon;
        [Tooltip("Displays IBuyable.ItemName.")]
        [SerializeField] private TMP_Text titleText;
        [Tooltip("Displays IBuyable.ItemDescription.")]
        [SerializeField] private TMP_Text descriptionText;
        [Tooltip("Layout parent where offer-button prefabs are instantiated for each BuyBundle.")]
        [SerializeField] private Transform offerContainer;
        [Tooltip("Disabled template instantiated once for every BuyBundle on the product.")]
        [SerializeField] private MarketOfferButton offerButtonPrefab;

        private readonly List<MarketOfferButton> offerButtons = new List<MarketOfferButton>();
        private IBuyable listing;

        public void Bind(IBuyable buyable)
        {
            listing = buyable;
            if (titleText != null) titleText.text = buyable.ItemName;
            if (descriptionText != null) descriptionText.text = buyable.ItemDescription;
            if (icon != null && buyable.BuyConfig != null && buyable.BuyConfig.Count > 0) icon.sprite = buyable.BuyConfig[0].buySprite;

            ClearOffers();
            if (offerButtonPrefab == null || offerContainer == null || buyable.BuyConfig == null) return;
            foreach (IBuyable.BuyBundle bundle in buyable.BuyConfig)
            {
                MarketOfferButton offer = Instantiate(offerButtonPrefab, offerContainer);
                offer.gameObject.SetActive(true);
                offer.Bind(buyable, bundle, gameObject);
                offerButtons.Add(offer);
            }
        }

        public void RefreshPurchaseStates()
        {
            foreach (MarketOfferButton offer in offerButtons)
                if (offer != null) offer.RefreshState();
        }

        private void ClearOffers()
        {
            foreach (MarketOfferButton offer in offerButtons)
                if (offer != null) Destroy(offer.gameObject);
            offerButtons.Clear();
        }
    }
}
