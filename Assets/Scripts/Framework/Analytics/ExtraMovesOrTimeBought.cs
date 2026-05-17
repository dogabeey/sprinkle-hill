namespace Game
{
    // Moves Bought Event
    public class ExtraMovesOrTimeBought : Unity.Services.Analytics.Event
    {
        public int LevelIndex { set { SetParameter("levelIndex", value); } }
        public int CashAmount { set { SetParameter("cashAmount", value); } }
        public int ExtraMoves { set { SetParameter("extraMoves", value); } }
        public ExtraMovesOrTimeBought() : base("movesBought")
        {
        }
        public ExtraMovesOrTimeBought(int levelIndex, int cashAmount, int extraMoves) : base("movesBought")
        {
            LevelIndex = levelIndex;
            CashAmount = cashAmount;
            ExtraMoves = extraMoves;
        }
    }
}