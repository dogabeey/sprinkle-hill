using UnityEngine;
using UnityEngine.Events;

namespace Game
{
    public class TutorialStep
    {
        public string Id;
        public HighlightSelector highlightSelector;
        public GameEvent startEvent;
        public EventParams startEventExpectedParams;
        public EventParam startEventExpectedParamValues;
        public GameEvent completionEvent;
        public EventParams completionEventExpectedParams;
        public EventParam completionEventExpectedParamValues;
        public UnityAction onStart;
        public UnityAction onComplete;

        public bool isCompleted;

    }

    public abstract class HighlightSelector
    {
        public abstract GameObject[] HighlightedObjects { get; }
    }

    public class HighlightFirstMatchableElement : HighlightSelector
    {
        public override GameObject[] HighlightedObjects
        {
            get
            {
                // Implement logic to return the first matchable element(s)
                return new GameObject[0];
            }
        }
    }
}
