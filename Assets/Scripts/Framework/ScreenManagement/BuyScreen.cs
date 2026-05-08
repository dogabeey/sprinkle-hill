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

        public IBuyable ReferenceBuyable
        {
            get
            {
                return referenceBuyable;
            }
            set
            {
                referenceBuyable = value;
                Init(value);
            }
        }

        public override void InitUI()
        {

        }

        public void Init(IBuyable referenceBuyable)
        {
            this.referenceBuyable = referenceBuyable;
            foreach (Transform child in buyScreenNodeContainer.transform)
            {
                Destroy(child.gameObject);
            }

            if (itemHeaderText != null)
            {
                itemHeaderText.text = referenceBuyable.ActionName;
            }

            // Use IBuyable.BuyChoices to populate the buy screen with BuyScreenNodes.
            foreach (int buyChoice in referenceBuyable.BuyChoices)
            {
                BuyScreenNode newNode = Instantiate(buyScreenNodePrefab, buyScreenNodeContainer.transform);
                newNode.Init(referenceBuyable, buyChoice);
            }
        }
    }
}

