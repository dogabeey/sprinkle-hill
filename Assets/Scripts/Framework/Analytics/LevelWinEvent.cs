namespace Game
{
    // Level Win Event
    public class LevelWinEvent : Unity.Services.Analytics.Event
    {
        public int LevelIndex { set { SetParameter("levelIndex", value); } }
        public int Moves { set { SetParameter("moves", value); } }


        public LevelWinEvent() : base("levelWin")
        {

        }
        public LevelWinEvent(int levelIndex, int moves) : base("levelWin")
        {
            LevelIndex = levelIndex;
            Moves = moves;
        }
    }
}