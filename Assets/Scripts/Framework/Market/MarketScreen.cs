using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Game.EventManagement;

namespace Game
{
    /// <summary>Inspector-driven market screen. Configure its category containers on the prefab.</summary>
    public sealed class MarketScreen : GameScreen
    {
        [Header("Prefab References")]
        [Tooltip("Disabled template instantiated once for every product shown in a category.")]
        [SerializeField] private MarketListingView listingPrefab;
        [Tooltip("Closes the market and returns to the previous non-persistent screen.")]
        [SerializeField] private Button closeButton;
        [Tooltip("Shown when no configured product can be placed in any category container.")]
        [SerializeField] private GameObject emptyState;
        [Tooltip("One container per MarketCategory. Products without a matching container are skipped.")]
        [SerializeField] private List<MarketCategoryContainer> categoryContainers = new List<MarketCategoryContainer>();

        private readonly List<MarketListingView> activeListings = new List<MarketListingView>();
        public override Screens ScreenID => Screens.Market;

        private void Awake()
        {
            if (closeButton != null)
                closeButton.onClick.AddListener(Close);
        }

        private void OnEnable()
        {
            if (MarketManager.Instance != null)
                MarketManager.Instance.ListingsChanged += Rebuild;
            EventManager.StartListening(GameEvent.CURRENCY_CHANGED, OnCurrencyChanged);
        }

        private void OnDisable()
        {
            if (MarketManager.Instance != null)
                MarketManager.Instance.ListingsChanged -= Rebuild;
            EventManager.StopListening(GameEvent.CURRENCY_CHANGED, OnCurrencyChanged);
        }

        public override void InitUI(EventParam eventParam)
        {
            base.InitUI(eventParam);
            MarketManager.Instance?.RefreshListings();
            Rebuild();
        }

        public override void ResolveParams(EventParam eventParam) { }

        private void Rebuild()
        {
            ClearListings();
            if (listingPrefab == null || MarketManager.Instance == null)
                return;

            foreach (IBuyable listing in MarketManager.Instance.Listings)
            {
                MarketCategoryContainer container = categoryContainers.Find(item => item != null && item.Category == listing.ItemCategory);
                if (container == null || container.Content == null)
                {
                    Debug.LogWarning($"Market category '{listing.ItemCategory}' has no configured container.", this);
                    continue;
                }

                MarketListingView view = Instantiate(listingPrefab, container.Content);
                view.gameObject.SetActive(true);
                view.Bind(listing);
                activeListings.Add(view);
            }

            foreach (MarketCategoryContainer container in categoryContainers)
                if (container != null) container.RefreshVisibility();
            if (emptyState != null) emptyState.SetActive(activeListings.Count == 0);
        }

        private void OnCurrencyChanged(EventParam eventParam)
        {
            foreach (MarketListingView listing in activeListings)
                if (listing != null) listing.RefreshPurchaseStates();
        }

        private void ClearListings()
        {
            foreach (MarketListingView listing in activeListings)
                if (listing != null) Destroy(listing.gameObject);
            activeListings.Clear();
        }

        private void Close() => ScreenManager.Instance.CloseAllNonPersistentScreens();
    }
}
