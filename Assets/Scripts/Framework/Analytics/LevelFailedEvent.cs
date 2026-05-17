namespace Game
{
    // Level Fail Event
    public class LevelFailedEvent : Unity.Services.Analytics.Event
    {
        public int LevelIndex { set { SetParameter("levelIndex", value); } }

        public LevelFailedEvent() : base("levelFail")
        {
        }
        public LevelFailedEvent(int levelIndex) : base("levelFail")
        {
            LevelIndex = levelIndex;
        }
    }
}