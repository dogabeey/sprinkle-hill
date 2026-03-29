using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using System.Collections.Generic;
namespace Game
{
    public class TutorialManager : MonoBehaviour
    {
        public List<TutorialStep> tutorialSteps = new List<TutorialStep>();
        private void OnEnable()
        {
            foreach (var step in tutorialSteps)
            {
                EventManager.StartListening(step.StartEvent, (EventParam e) => StartTutorialStep(step));
                EventManager.StartListening(step.CompletionEvent, (EventParam e) => CompleteTutorialStep(step));
            }
        }
        private void OnDisable()
        {
            foreach (var step in tutorialSteps)
            {
                EventManager.StopListening(step.StartEvent, (EventParam e) => StartTutorialStep(step));
                EventManager.StopListening(step.CompletionEvent, (EventParam e) => CompleteTutorialStep(step));
            }
        }
        private void StartTutorialStep(TutorialStep step)
        {
            if (!step.isCompleted)
            {
                step.OnStart?.Invoke();
            }
        }
        private void CompleteTutorialStep(TutorialStep step)
        {
            if (!step.isCompleted)
            {
                step.isCompleted = true;
                step.OnComplete?.Invoke();
            }
        }
    }

    [System.Serializable]
    public abstract class TutorialStep
    {
        public string Id;
        public abstract GameEvent StartEvent { get; }
        public abstract GameEvent CompletionEvent { get; }
        public abstract UnityAction OnStart { get; }
        public abstract UnityAction OnComplete { get; }

        public bool isCompleted;

        public abstract GameObject[] HighlightedObjects { get; }
    }
}

