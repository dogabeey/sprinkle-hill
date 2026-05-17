namespace Game
{
    // Swap Failed Event: Sent when a swap action does not result in a match
    public class SwapFailedEvent : Unity.Services.Analytics.Event
    {
        public int LevelIndex { set { SetParameter("levelIndex", value); } }
        public string ElementName { set { SetParameter("elementName", value); } }

        public SwapFailedEvent() : base("swapFailed")
        {
        }
        public SwapFailedEvent(int levelIndex, string elementName) : base("swapFailed")
        {
            LevelIndex = levelIndex;
            ElementName = elementName;
        }
    }
}