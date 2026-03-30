using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Game
{
    public class TutorialManager : SerializedMonoBehaviour
    {
        [System.Serializable]
        public class TutorialStep
        {
            public string Id;
            [SerializeReference]
            public TutorialStep nextStep;
            [SerializeReference]
            public HighlightSelector highlightSelector;
            public GameEvent startEvent;
            [ShowIf(nameof(IsAdvancedMode))]
            public EventParams startEventExpectedParams;
            [ShowIf(nameof(IsAdvancedMode))]
            public EventParam startEventExpectedParamValues;
            public GameEvent completionEvent;
            [ShowIf(nameof(IsAdvancedMode))]
            public EventParams completionEventExpectedParams;
            [ShowIf(nameof(IsAdvancedMode))]
            public EventParam completionEventExpectedParamValues;
            public UnityAction onStart;
            public UnityAction onComplete;
            public bool isCompleted;
            public bool advancedMode;

            public bool IsAdvancedMode() => advancedMode;
        }

        public List<TutorialStep> tutorialSteps = new List<TutorialStep>();

        [Header("Highlight Overlay")]
        [Tooltip("Assign the TutorialHighlightOverlay component. Leave null to skip highlighting.")]
        public TutorialHighlightOverlay highlightOverlay;

        // Stored delegates so StopListening receives the same instance as StartListening.
        private readonly Dictionary<TutorialStep, Action<EventParam>> _startListeners      = new Dictionary<TutorialStep, Action<EventParam>>();
        private readonly Dictionary<TutorialStep, Action<EventParam>> _completionListeners  = new Dictionary<TutorialStep, Action<EventParam>>();

        private void OnEnable()
        {
            foreach (TutorialStep step in tutorialSteps)
            {
                TutorialStep captured = step;

                Action<EventParam> onStart = e =>
                {
                    if (ParamMatches(e, captured.startEventExpectedParams, captured.startEventExpectedParamValues))
                        StartTutorialStep(captured);
                };

                Action<EventParam> onComplete = e =>
                {
                    if (ParamMatches(e, captured.completionEventExpectedParams, captured.completionEventExpectedParamValues))
                        CompleteTutorialStep(captured);
                };

                _startListeners[step]     = onStart;
                _completionListeners[step] = onComplete;

                EventManager.StartListening(step.startEvent,      onStart);
                EventManager.StartListening(step.completionEvent, onComplete);
            }
        }

        private void OnDisable()
        {
            foreach (TutorialStep step in tutorialSteps)
            {
                if (_startListeners.TryGetValue(step, out Action<EventParam> onStart))
                    EventManager.StopListening(step.startEvent, onStart);

                if (_completionListeners.TryGetValue(step, out Action<EventParam> onComplete))
                    EventManager.StopListening(step.completionEvent, onComplete);
            }

            _startListeners.Clear();
            _completionListeners.Clear();
        }

        private void StartTutorialStep(TutorialStep step)
        {
            if (step.isCompleted) return;

            step.onStart?.Invoke();
            ShowOverlay(step);
        }

        private void CompleteTutorialStep(TutorialStep step)
        {
            if (step.isCompleted) return;

            step.isCompleted = true;
            step.onComplete?.Invoke();
            highlightOverlay?.Hide();

            if (step.nextStep != null)
            {
                StartTutorialStep(step.nextStep);
            }
        }

        private void ShowOverlay(TutorialStep step)
        {
            if (highlightOverlay == null) return;

            GameObject[] targets = step.highlightSelector.HighlightedObjects;
            if (targets == null || targets.Length == 0)
            {
                highlightOverlay.Hide();
                return;
            }

            highlightOverlay.Show(targets);
        }

        // ------------------------------------------------------------------
        //  Param matching
        // ------------------------------------------------------------------

        /// <summary>
        /// Returns true when every flag set in <paramref name="mask"/> has a matching
        /// value in the incoming <paramref name="incoming"/> param.
        /// If <paramref name="mask"/> is zero (no flags), always returns true.
        /// </summary>
        private static bool ParamMatches(EventParam incoming, EventParams mask, EventParam expected)
        {
            if (mask == 0 || expected == null) return true;

            if ((mask & EventParams.IntValue) != 0 && incoming.paramInt != expected.paramInt)
                return false;

            if ((mask & EventParams.FloatValue) != 0 && !Mathf.Approximately(incoming.paramFloat, expected.paramFloat))
                return false;

            if ((mask & EventParams.StringValue) != 0 && incoming.paramStr != expected.paramStr)
                return false;

            if ((mask & EventParams.BoolValue) != 0 && incoming.paramBool != expected.paramBool)
                return false;

            return true;
        }
    }

    [System.Flags]
    public enum EventParams
    {
        None = 0,
        IntValue    = 1 << 1,
        FloatValue  = 1 << 2,
        StringValue = 1 << 3,
        BoolValue   = 1 << 4,
    }
}

