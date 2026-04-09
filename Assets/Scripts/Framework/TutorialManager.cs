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
    public partial class TutorialManager : SerializedMonoBehaviour
    {
        public List<TutorialStep> tutorialSteps = new List<TutorialStep>();
        public float directiveParentHeight;
        public Transform animationObjectParent; // Parent for tutorial animation objects that is set at tutorialAnimation.tutorialObject. If null, animations will be parented to the first canvas in the scene.
        public TMP_Text directiveText;
        public RectTransform directiveParent;
        [Tooltip("Assign the TutorialHighlightOverlay component. Leave null to skip highlighting.")]
        public TutorialHighlightOverlay highlightOverlay;

        // Stored delegates so StopListening receives the same instance as StartListening.
        private readonly Dictionary<TutorialStep, Action<EventParam>> _startListeners      = new Dictionary<TutorialStep, Action<EventParam>>();
        private readonly Dictionary<TutorialStep, Action<EventParam>> _completionListeners  = new Dictionary<TutorialStep, Action<EventParam>>();
        private TutorialStep _activeStep;
        private bool _tutorialListenersRegistered;
        private bool _isLevelEnded;

        public bool HasActiveStep => _activeStep != null && _activeStep.isStarted && !_activeStep.isCompleted;
        public bool ShouldDisableActionBar => HasActiveStep && _activeStep.disablesActionBar;

        private void OnEnable()
        {
            UpdateSerializationDepths();

            EventManager.StartListening(GameEvent.LEVEL_COMPLETED, OnLevelEnded);
            EventManager.StartListening(GameEvent.LEVEL_FAILED, OnLevelEnded);
            EventManager.StartListening(GameEvent.LEVEL_STARTED, OnLevelStarted);

            RegisterTutorialListeners();
        }

        private void OnDisable()
        {
            EventManager.StopListening(GameEvent.LEVEL_COMPLETED, OnLevelEnded);
            EventManager.StopListening(GameEvent.LEVEL_FAILED, OnLevelEnded);
            EventManager.StopListening(GameEvent.LEVEL_STARTED, OnLevelStarted);

            UnregisterTutorialListeners();
            EndAllTutorialSteps();
            _isLevelEnded = false;
        }

        private void RegisterTutorialListeners()
        {
            if (_tutorialListenersRegistered)
                return;

            foreach (TutorialStep step in GetAllConfiguredSteps())
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

            EventManager.StartListening(GameEvent.HIGHLIGHT_UPDATED, OnHighlightUpdated);
            _tutorialListenersRegistered = true;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            UpdateSerializationDepths();
        }
#endif

        private void UpdateSerializationDepths()
        {
            foreach (TutorialStep step in GetAllConfiguredSteps())
            {
                step.serializationDepth = -1;
            }

            for (int i = 0; i < tutorialSteps.Count; i++)
            {
                AssignSerializationDepth(tutorialSteps[i], 0, new HashSet<TutorialStep>());
            }
        }

        private void AssignSerializationDepth(TutorialStep step, int depth, HashSet<TutorialStep> currentPath)
        {
            if (step == null || !currentPath.Add(step))
                return;

            if (step.serializationDepth < 0 || depth < step.serializationDepth)
            {
                step.serializationDepth = depth;
            }

            AssignSerializationDepth(step.nextStep, depth + 1, currentPath);
            currentPath.Remove(step);
        }

        private void UnregisterTutorialListeners()
        {
            if (!_tutorialListenersRegistered)
                return;

            foreach (TutorialStep step in GetAllConfiguredSteps())
            {
                if (_startListeners.TryGetValue(step, out Action<EventParam> onStart))
                    EventManager.StopListening(step.startEvent, onStart);

                if (_completionListeners.TryGetValue(step, out Action<EventParam> onComplete))
                    EventManager.StopListening(step.completionEvent, onComplete);
            }

            EventManager.StopListening(GameEvent.HIGHLIGHT_UPDATED, OnHighlightUpdated);

            _startListeners.Clear();
            _completionListeners.Clear();
            _tutorialListenersRegistered = false;
        }

        private void OnLevelEnded(EventParam param)
        {
            _isLevelEnded = true;
            UnregisterTutorialListeners();
            EndAllTutorialSteps();
        }

        private void OnLevelStarted(EventParam param)
        {
            _isLevelEnded = false;
            ResetTutorialSteps();
            RegisterTutorialListeners();
        }

        private void EndAllTutorialSteps()
        {
            foreach (TutorialStep step in GetAllConfiguredSteps())
            {
                step.isStarted = false;
                step.isCompleted = true;
                ClearTutorialAnimation(step);
            }

            _activeStep = null;
            highlightOverlay?.Hide();
            ClearDirective();
        }

        private void ResetTutorialSteps()
        {
            foreach (TutorialStep step in GetAllConfiguredSteps())
            {
                step.isStarted = false;
                step.isCompleted = false;
                ClearTutorialAnimation(step);
            }

            _activeStep = null;
            highlightOverlay?.Hide();
            ClearDirective();
        }

        private IEnumerable<TutorialStep> GetAllConfiguredSteps()
        {
            HashSet<TutorialStep> visitedSteps = new HashSet<TutorialStep>();
            Stack<TutorialStep> pendingSteps = new Stack<TutorialStep>(tutorialSteps);

            while (pendingSteps.Count > 0)
            {
                TutorialStep step = pendingSteps.Pop();

                if (step == null || !visitedSteps.Add(step))
                    continue;

                yield return step;

                if (step.nextStep != null)
                    pendingSteps.Push(step.nextStep);
            }
        }

        private void StartTutorialStep(TutorialStep step)
        {
            if (_isLevelEnded) return;
            if (step == null || step.isCompleted || step.isStarted) return;
            if (!IsStepEligibleForCurrentLevel(step)) return;
            if (!IsStepEligibleForCurrentStage(step)) return;

            step.isStarted = true;
            _activeStep = step;
            step.onStart?.Invoke();
            ShowOverlay(step);
            ShowDirective(step);
        }

        private void OnHighlightUpdated(EventParam param)
        {
            if (_isLevelEnded)
                return;

            if (_activeStep == null || _activeStep.isCompleted)
                return;

            PlayTutorialAnimation(_activeStep);
        }

        private void CompleteTutorialStep(TutorialStep step)
        {
            if (_isLevelEnded) return;
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
                DOVirtual.DelayedCall(0.02f, () =>
                {
                    if (_isLevelEnded)
                        return;

                    StartTutorialStep(step.nextStep);
                });
            }
        }

        private static bool IsStepEligibleForCurrentLevel(TutorialStep step)
        {
            if (step == null || step.requiredLevelIndex < 0)
                return true;

            if (World.Instance == null)
                return false;

            return World.Instance.lastPlayedLevelIndex == step.requiredLevelIndex;
        }

        private static bool IsStepEligibleForCurrentStage(TutorialStep step)
        {
            if (step == null || step.requiredStageIndex < 0)
                return true;

            LevelScene_Match3Game levelScene = World.Instance != null ? World.Instance.CurrentLevel as LevelScene_Match3Game : null;
            if (levelScene == null)
                return false;

            return levelScene.CurrentStageIndex == step.requiredStageIndex;
        }

        public void ShowDirective(TutorialStep step)
        {
            if (directiveText != null)
            {
                directiveText.text = step.directive;
                directiveParent.gameObject.SetActive(true);
                directiveParent.anchoredPosition = new Vector2(directiveParent.anchoredPosition.x, directiveParentHeight);
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
            if (animationObjectParent != null)
            {
                animationInstance = Instantiate(step.tutorialAnimation.tutorialObject, animationObjectParent);
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

        public bool IsElementInteractionAllowed(GameObject candidate)
        {
            if (candidate == null)
                return false;

            TutorialStep step = _activeStep;
            if (step == null || !step.isStarted || step.isCompleted)
                return true;

            GameObject[] highlighted = step.highlightSelector != null ? step.highlightSelector.HighlightedObjects : null;
            if (highlighted == null || highlighted.Length == 0)
                return true;

            List<GameObject> highlightedElements = new List<GameObject>();
            for (int i = 0; i < highlighted.Length; i++)
            {
                GameObject target = highlighted[i];
                if (target == null)
                    continue;

                if (target.GetComponentInParent<GridElement_Match3Game>() != null)
                    highlightedElements.Add(target);
            }

            if (highlightedElements.Count == 0)
                return true;

            GridElement_Match3Game candidateElement = candidate.GetComponentInParent<GridElement_Match3Game>();
            if (candidateElement == null)
                return false;

            for (int i = 0; i < highlightedElements.Count; i++)
            {
                GridElement_Match3Game highlightedElement = highlightedElements[i] != null
                    ? highlightedElements[i].GetComponentInParent<GridElement_Match3Game>()
                    : null;

                if (highlightedElement == null)
                    continue;

                if (highlightedElement == candidateElement)
                    return true;
            }

            return false;
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

