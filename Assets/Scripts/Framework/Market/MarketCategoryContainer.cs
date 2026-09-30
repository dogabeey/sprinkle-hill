using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Game
{
    public sealed class MarketCategoryContainer : MonoBehaviour, IPointerClickHandler
    {
        [Tooltip("Only products with this category are displayed in this container.")]
        [SerializeField] private MarketCategory category;
        [Tooltip("The tab toggle that selects this category. If omitted, a Toggle on this object is used.")]
        [SerializeField] private Toggle categoryToggle;
        [Tooltip("Layout parent where the MarketScreen instantiates listing-view prefabs.")]
        [SerializeField] private Transform content;
        [Tooltip("Optional object shown when this category has no listings.")]
        [SerializeField] private GameObject emptyState;

        public MarketCategory Category => category;
        public Transform Content => content;

        private void Awake()
        {
            if (categoryToggle == null)
                TryGetComponent(out categoryToggle);
        }

        private void OnEnable()
        {
            if (categoryToggle != null)
                categoryToggle.onValueChanged.AddListener(OnToggleValueChanged);
        }

        private void OnDisable()
        {
            if (categoryToggle != null)
                categoryToggle.onValueChanged.RemoveListener(OnToggleValueChanged);
        }

        public void ConfigureToggle(ToggleGroup toggleGroup)
        {
            if (categoryToggle == null)
                TryGetComponent(out categoryToggle);

            if (categoryToggle != null)
                categoryToggle.group = toggleGroup;
        }

        public void SetSelected(bool selected)
        {
            if (categoryToggle != null)
                categoryToggle.SetIsOnWithoutNotify(selected);
        }

        // This can also be assigned to a Button's OnClick event when a custom tab has no Toggle.
        public void Select()
        {
            MarketManager.Instance?.SelectCategory(category);
        }

        private void OnToggleValueChanged(bool isOn)
        {
            if (isOn)
                Select();
        }

        // Some existing category prefabs render their icon beside the Toggle rather than inside it.
        // Receiving the bubbled click here keeps the whole tab clickable in either hierarchy.
        public void OnPointerClick(PointerEventData eventData) => Select();

        public void RefreshVisibility()
        {
            if (emptyState != null)
                emptyState.SetActive(content == null || content.childCount == 0);
        }
    }
}
