using TMPro;

namespace Game
{
    public class BuyScreen : GameScreen
    {
        public override Screens ScreenID => Screens.BuyMenu;

        internal IBuyable referenceBuyable;

        public TMP_Text itemHeaderText;

        public override void InitUI()
        {
            throw new System.NotImplementedException();
        }
    }

    public class BuyData
    {
        public IBuyable buyableItem;
        public int buyAmount;
    }
}

