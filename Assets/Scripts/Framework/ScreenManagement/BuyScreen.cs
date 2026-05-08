using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game
{
    public class BuyScreen : GameScreen
    {
        public override Screens ScreenID => Screens.BuyMenu;

        private IBuyable referenceBuyable;

        public BuyScreenNode buyScreenNodePrefab;
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

            if (itemHeaderText) itemHeaderText.text = referenceBuyable.ActionName;
            if (itemDescriptionText) itemDescriptionText.text = referenceBuyable.ActionDescription;

            // Use IBuyable.BuyConfig to populate the buy screen with BuyScreenNodes.
            foreach (var buyBundle in referenceBuyable.BuyConfig)
            {
                BuyScreenNode newNode = Instantiate(buyScreenNodePrefab, buyScreenNodeContainer.transform);
                newNode.Init(referenceBuyable, buyBundle.buyCount);
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

