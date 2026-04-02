using DG.Tweening;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Game
{
    public class TutorialManager : SerializedMonoBehaviour
    {
        [System.Serializable]
        public class TutorialStep
        {
            public string Id;
            public string directive;
            [SerializeReference]
            public TutorialStep nextStep;
            [SerializeReference]
            public TutorialAnimation tutorialAnimation;
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
            public bool isStarted;
            public bool isCompleted;
            public bool advancedMode;
            [Header("Scene References")]
            public Transform animationObjectParent; // Parent for tutorial animation objects that is set at tutorialAnimation.tutorialObject. If null, animations will be parented to the first canvas in the scene.

            public bool IsAdvancedMode() => advancedMode;
        }

        public List<TutorialStep> tutorialSteps = new List<TutorialStep>();

        public TMP_Text directiveText;
        public Transform directiveParent;
        [Tooltip("Assign the TutorialHighlightOverlay component. Leave null to skip highlighting.")]
        public TutorialHighlightOverlay highlightOverlay;

        // Stored delegates so StopListening receives the same instance as StartListening.
        private readonly Dictionary<TutorialStep, Action<EventParam>> _startListeners      = new Dictionary<TutorialStep, Action<EventParam>>();
        private readonly Dictionary<TutorialStep, Action<EventParam>> _completionListeners  = new Dictionary<TutorialStep, Action<EventParam>>();
        private TutorialStep _activeStep;

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

                if(step.nextStep != null) // Also register the next step's completion event as they are not in the list of tutorialSteps.
                {

                    Action<EventParam> onCompleteNext = e =>
                    {
                        if (ParamMatches(e, step.nextStep.completionEventExpectedParams, step.nextStep.completionEventExpectedParamValues))
                            CompleteTutorialStep(step.nextStep);
                    };

                    _completionListeners[step.nextStep] = onCompleteNext;
                    if (step.nextStep != null)
                        EventManager.StartListening(step.nextStep.completionEvent, onCompleteNext);
                }

                _startListeners[step]     = onStart;
                _completionListeners[step] = onComplete;

                EventManager.StartListening(step.startEvent,      onStart);
                EventManager.StartListening(step.completionEvent, onComplete);
            }

            EventManager.StartListening(GameEvent.HIGHLIGHT_UPDATED, OnHighlightUpdated);
        }

        private void OnDisable()
        {
            foreach (TutorialStep step in tutorialSteps)
            {
                if (_startListeners.TryGetValue(step, out Action<EventParam> onStart))
                    EventManager.StopListening(step.startEvent, onStart);

                if (_completionListeners.TryGetValue(step, out Action<EventParam> onComplete))
                    EventManager.StopListening(step.completionEvent, onComplete);

                if (step.nextStep != null && _completionListeners.TryGetValue(step.nextStep, out Action<EventParam> onCompleteNext))
                    EventManager.StopListening(step.nextStep.completionEvent, onCompleteNext);

                ClearTutorialAnimation(step);
            }

            _activeStep = null;
            EventManager.StopListening(GameEvent.HIGHLIGHT_UPDATED, OnHighlightUpdated);

            _startListeners.Clear();
            _completionListeners.Clear();
        }

        private void StartTutorialStep(TutorialStep step)
        {
            if (step == null || step.isCompleted || step.isStarted) return;

            step.isStarted = true;
            _activeStep = step;
            step.onStart?.Invoke();
            ShowOverlay(step);
            ShowDirective(step);
        }

        private void OnHighlightUpdated(EventParam param)
        {
            if (_activeStep == null || _activeStep.isCompleted)
                return;

            PlayTutorialAnimation(_activeStep);
        }

        private void CompleteTutorialStep(TutorialStep step)
        {
            if (step == null || !step.isStarted || step.isCompleted) return;

            step.isCompleted = true;
            step.isStarted = false;
            step.onComplete?.Invoke();
            highlightOverlay?.Hide();
            ClearTutorialAnimation(step);
            ClearDirective();

            if (_activeStep == step)
                _activeStep = null;

            if (step.nextStep != null)
            {
                DOVirtual.DelayedCall(0.02f, () => StartTutorialStep(step.nextStep));
            }
        }

        public void ShowDirective(TutorialStep step)
        {
            if (directiveText != null)
            {
                directiveText.text = step.directive;
                directiveParent.gameObject.SetActive(true);
            }
        }
        public void ClearDirective()
        {
            if (directiveText != null)
            {
                directiveText.text = "";
                directiveParent.gameObject.SetActive(false);
            }
        }

        private void ShowOverlay(TutorialStep step)
        {
            GameObject[] targets = step?.highlightSelector?.HighlightedObjects;
            bool hasTargets = targets != null && targets.Length > 0;

            if (highlightOverlay != null)
            {
                if (!hasTargets)
                {
                    highlightOverlay.Hide();
                }
                else
                {
                    highlightOverlay.Show(targets);
                }
            }

            if (!hasTargets)
            {
                ClearTutorialAnimation(step);
            }
        }

        private void PlayTutorialAnimation(TutorialStep step)
        {
            if (step?.tutorialAnimation == null || step.tutorialAnimation.tutorialObject == null)
                return;

            ClearTutorialAnimation(step);

            GameObject animationInstance;
            if (step.animationObjectParent != null)
            {
                animationInstance = Instantiate(step.tutorialAnimation.tutorialObject, step.animationObjectParent);
            }
            else
            {
                Canvas firstCanvas = FindObjectOfType<Canvas>();
                animationInstance = firstCanvas != null
                    ? Instantiate(step.tutorialAnimation.tutorialObject, firstCanvas.transform)
                    : Instantiate(step.tutorialAnimation.tutorialObject);
            }

            step.tutorialAnimation.Initialize(step, animationInstance);
            step.tutorialAnimation.PlayAnim();
        }

        private void ClearTutorialAnimation(TutorialStep step)
        {
            step?.tutorialAnimation?.ClearAnim();
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

