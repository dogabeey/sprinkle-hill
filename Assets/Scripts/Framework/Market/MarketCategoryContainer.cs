using UnityEngine;

namespace Game
{
    public sealed class MarketCategoryContainer : MonoBehaviour
    {
        [Tooltip("Only products with this category are displayed in this container.")]
        [SerializeField] private MarketCategory category;
        [Tooltip("Layout parent where the MarketScreen instantiates listing-view prefabs.")]
        [SerializeField] private Transform content;
        [Tooltip("Optional object shown when this category has no listings.")]
        [SerializeField] private GameObject emptyState;

        public MarketCategory Category => category;
        public Transform Content => content;

        public void RefreshVisibility()
        {
            if (emptyState != null)
                emptyState.SetActive(content == null || content.childCount == 0);
        }
    }
}
