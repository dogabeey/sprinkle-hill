namespace Game
{
    // Booster Bought Event
    public class BoosterBoughtEvent : Unity.Services.Analytics.Event
    {
        public int LevelIndex { set { SetParameter("levelIndex", value); } }
        public string BoosterName { set { SetParameter("boosterName", value); } }
        public int CashAmount { set { SetParameter("cashAmount", value); } }
        public int ItemAmount { set { SetParameter("itemAmount", value); } }
        public BoosterBoughtEvent() : base("boosterBought")
        {
        }
        public BoosterBoughtEvent(int levelIndex, string boosterName, int cashAmount, int itemAmount) : base("boosterBought")
        {
            LevelIndex = levelIndex;
            BoosterName = boosterName;
            CashAmount = cashAmount;
            ItemAmount = itemAmount;
        }
    }
}