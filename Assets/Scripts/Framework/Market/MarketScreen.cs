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
        
        [Tooltip("One container per MarketCategory. Products without a matching container are skipped.")]
        [SerializeField] private List<MarketCategoryContainer> categoryContainers = new List<MarketCategoryContainer>();
        [Header("Scene References")]
        [Tooltip("Parent of the market listings.")]
        [SerializeField] private Transform listingParent;
        [Tooltip("Parent of the market categories.")]
        [SerializeField] private Transform categoryParent;
        [Tooltip("The group shared by every category tab. If omitted, the group on Category Parent is used.")]
        [SerializeField] private ToggleGroup categoryToggleGroup;
        [Tooltip("Shown when no configured product can be placed in any category container.")]
        [SerializeField] private GameObject emptyState;
        [Tooltip("Closes the market and returns to the previous non-persistent screen.")]
        [SerializeField] private Button closeButton;
        private readonly List<MarketListingView> activeListings = new List<MarketListingView>();
        public override Screens ScreenID => Screens.Market;

        private void Awake()
        {
            if (closeButton != null)
                closeButton.onClick.AddListener(Close);

            ConfigureCategoryTabs();
        }

        private void OnEnable()
        {
            if (MarketManager.Instance != null)
            {
                MarketManager.Instance.ListingsChanged += Rebuild;
                MarketManager.Instance.CategoryChanged += OnCategoryChanged;
            }
            EventManager.StartListening(GameEvent.CURRENCY_CHANGED, OnCurrencyChanged);
        }

        private void OnDisable()
        {
            if (MarketManager.Instance != null)
            {
                MarketManager.Instance.ListingsChanged -= Rebuild;
                MarketManager.Instance.CategoryChanged -= OnCategoryChanged;
            }
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
                if (container == null || listingParent == null)
                {
                    Debug.LogWarning($"Market category '{listing.ItemCategory}' has no configured container or Listing Parent.", this);
                    continue;
                }

                MarketListingView view = Instantiate(listingPrefab, listingParent);
                view.gameObject.SetActive(true);
                view.Bind(listing);
                activeListings.Add(view);
            }

            foreach (MarketCategoryContainer container in categoryContainers)
                if (container != null) container.RefreshVisibility();
            ApplyCategoryVisibility();
        }

        private void OnCategoryChanged(MarketCategory category) => ApplyCategoryVisibility();

        private void ApplyCategoryVisibility()
        {
            MarketManager manager = MarketManager.Instance;
            if (manager == null)
                return;

            int visibleListingCount = 0;
            foreach (MarketListingView listing in activeListings)
            {
                if (listing == null)
                    continue;

                bool isSelectedCategory = listing.Category == manager.CurrentCategory;
                listing.gameObject.SetActive(isSelectedCategory);
                if (isSelectedCategory)
                    visibleListingCount++;
            }

            foreach (MarketCategoryContainer container in categoryContainers)
                if (container != null) container.SetSelected(container.Category == manager.CurrentCategory);

            if (emptyState != null)
                emptyState.SetActive(visibleListingCount == 0);
        }

        private void ConfigureCategoryTabs()
        {
            if (categoryToggleGroup == null && categoryParent != null)
                categoryToggleGroup = categoryParent.GetComponent<ToggleGroup>();

            if (categoryToggleGroup == null && categoryParent != null)
                categoryToggleGroup = categoryParent.gameObject.AddComponent<ToggleGroup>();

            if (categoryToggleGroup != null)
                categoryToggleGroup.allowSwitchOff = false;

            foreach (MarketCategoryContainer container in categoryContainers)
                if (container != null) container.ConfigureToggle(categoryToggleGroup);
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

        private void Close() => ScreenManager.Instance.CloseAllScreens(false);
    }
}
