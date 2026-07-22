using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using Game.Singleton;

namespace Game
{
    /// <summary>
    /// Editor-configured source of all market listings. Add MarketProduct assets for currency
    /// packs and future products; action-bar items are included automatically when enabled.
    /// </summary>
    public sealed class MarketManager : SingletonComponent<MarketManager>
    {
        private const string ManagerResourcePath = "Market/Market Manager";
        private const string MarketPanelResourcePath = "Market/Market Panel";

        [Header("Listing Sources")]
        [Tooltip("When enabled, every ActionBarItem is shown in the Boosters market category.")]
        [SerializeField] private bool includeActionBarItems = true;
        [Tooltip("Additional ScriptableObject market products, such as currency packs. Create them with Create > Game > Market > Product.")]
        [SerializeField] private List<MarketProduct> configuredProducts = new List<MarketProduct>();

        private readonly List<IBuyable> listings = new List<IBuyable>();
        public IReadOnlyList<IBuyable> Listings => listings;
        public event Action ListingsChanged;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void CreateFromPrefab()
        {
            if (Exists())
                return;

            MarketManager prefab = Resources.Load<MarketManager>(ManagerResourcePath);
            if (prefab == null)
            {
                Debug.LogError($"Create a MarketManager prefab at Resources/{ManagerResourcePath}.");
                return;
            }

            Instantiate(prefab);
        }

        protected override void Awake()
        {
            base.Awake();
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            EnsureMarketScreen();
            RefreshListings();
        }

        public void RefreshListings()
        {
            listings.Clear();
            AddRange(configuredProducts);

            if (includeActionBarItems && ActionBarManager.Instance != null)
                AddRange(ActionBarManager.Instance.actionBarItemList);

            foreach (IBuyable buyable in listings)
            {
                if (buyable.BuyConfig == null)
                    continue;

                foreach (IBuyable.BuyBundle bundle in buyable.BuyConfig)
                    bundle.buyableReference = buyable;
            }

            ListingsChanged?.Invoke();
        }

        private void EnsureMarketScreen()
        {
            if (ScreenManager.Instance == null || ScreenManager.Instance.screens.Any(screen => screen != null && screen.ScreenID == Screens.Market))
                return;

            MarketScreen marketPrefab = Resources.Load<MarketScreen>(MarketPanelResourcePath);
            if (marketPrefab == null)
            {
                Debug.LogError($"Create a MarketScreen prefab at Resources/{MarketPanelResourcePath}.");
                return;
            }

            Canvas canvas = FindFirstObjectByType<Canvas>();
            MarketScreen marketScreen = Instantiate(marketPrefab, canvas != null ? canvas.transform : null);
            marketScreen.name = "Market Panel";
            marketScreen.gameObject.SetActive(false);
            ScreenManager.Instance.screens.Add(marketScreen);
        }

        private void AddRange(IEnumerable<IBuyable> buyables)
        {
            if (buyables == null) return;
            foreach (IBuyable buyable in buyables)
                if (buyable != null && !listings.Contains(buyable)) listings.Add(buyable);
        }
    }
}
