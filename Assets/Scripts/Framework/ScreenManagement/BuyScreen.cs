using TMPro;
using UnityEngine; using Game.EventManagement;
using UnityEngine.UI;

namespace Game
{
    public class BuyScreen : GameScreen
    {
        public override Screens ScreenID => Screens.BuyMenu;

        private IBuyable referenceBuyable;

        public BuyScreenNode buyScreenNodePrefab;
        public Button returnToGameButton;
        public LayoutGroup buyScreenNodeContainer;
        public TMP_Text itemHeaderText;
        public TMP_Text itemDescriptionText;

        public IBuyable ReferenceBuyable
        {
            get
            {
                return referenceBuyable;
            }
            set
            {
                referenceBuyable = value;
            }
        }

        public override void InitUI(EventParam eventParam)
        {
            base.InitUI(eventParam);
            foreach (Transform child in buyScreenNodeContainer.transform)
            {
                Destroy(child.gameObject);
            }

            if (itemHeaderText) itemHeaderText.text = referenceBuyable.ItemName;
            if (itemDescriptionText) itemDescriptionText.text = referenceBuyable.ItemDescription;
            if (returnToGameButton) returnToGameButton.onClick.AddListener(() => ScreenManager.Instance.CloseAllScreens());

            // Use IBuyable.BuyConfig to populate the buy screen with BuyScreenNodes.
            foreach (var buyBundle in referenceBuyable.BuyConfig)
            {
                buyBundle.buyableReference = referenceBuyable; // Set the buyable reference for each buy bundle.
                BuyScreenNode newNode = Instantiate(buyScreenNodePrefab, buyScreenNodeContainer.transform);
                newNode.Init(buyBundle);
            }
        }
        public override void ResolveParams(EventParam eventParam)
        {
            IBuyable buyableParam = eventParam.paramValue as IBuyable;
            if (buyableParam != null)
            {
                ReferenceBuyable = buyableParam;
            }
        }
    }
}

