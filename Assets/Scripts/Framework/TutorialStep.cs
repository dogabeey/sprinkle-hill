using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Game
{
    [CreateAssetMenu(menuName = "Game/Tutorial Step...", fileName = "TutorialStep")]
    [InlineEditor]
    public class TutorialStep : ScriptableObject
    {
        public string Id;
        public string directive;
        public float directiveParentHeight;
        public Vector2 anchorMin, anchorMax;
        [LabelText("Disable Action Bar")]
        public bool disablesActionBar = true;
        [Tooltip("-1 means any level. Otherwise this step only runs when lastPlayedLevelIndex equals this value.")]
        public int requiredLevelIndex = -1;
        [Tooltip("-1 means any stage. Otherwise this step only runs when currentStageIndex equals this value.")]
        public int requiredStageIndex = -1;  
        public TutorialAnimationType tutorialAnimationType;
        public TutorialAnimationSettings tutorialAnimationSettings = new TutorialAnimationSettings();
        public HighlightSelectorType highlightSelectorType;
        public HighlightSelectorSettings highlightSelectorSettings = new HighlightSelectorSettings();
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
        [FoldoutGroup("Custom Events")]
        public UnityEvent onStart;
        [FoldoutGroup("Custom Events")]
        public UnityEvent onComplete;
        [GUIColor(nameof(GetNextStepColor))]
        public TutorialStep nextStep;
        public bool advancedMode;
        [HideInInspector]
        public int serializationDepth;
        [HideInInspector]
        public bool isStarted;
        [HideInInspector]
        public bool isCompleted;

        [NonSerialized] private TutorialAnimation runtimeTutorialAnimation;
        [NonSerialized] private HighlightSelector runtimeHighlightSelector;

        public bool IsAdvancedMode() => advancedMode;

        private static readonly Color[] DepthColors =
        {
            Color.red,
            new Color(1f, 0.5f, 0f),
            Color.yellow,
            Color.green,
            Color.cyan,
            Color.blue,
            Color.magenta,
        };

        private Color GetNextStepColor()
        {
            int depth = Mathf.Max(0, serializationDepth + 1);
            return DepthColors[depth % DepthColors.Length];
        }

        public TutorialAnimation GetTutorialAnimation()
        {
            if (runtimeTutorialAnimation == null)
                runtimeTutorialAnimation = CreateTutorialAnimation();

            return runtimeTutorialAnimation;
        }

        public HighlightSelector GetHighlightSelector()
        {
            if (runtimeHighlightSelector == null)
                runtimeHighlightSelector = CreateHighlightSelector();

            return runtimeHighlightSelector;
        }

        public void RebuildRuntimeReferences()
        {
            runtimeTutorialAnimation = CreateTutorialAnimation();
            runtimeHighlightSelector = CreateHighlightSelector();
        }

        private TutorialAnimation CreateTutorialAnimation()
        {
            TutorialAnimation animation = tutorialAnimationType switch
            {
                TutorialAnimationType.MoveBetweenTwoPoint => new MoveBetweenTwoPoint(),
                TutorialAnimationType.ClickOnFirstHighlightedObject => new ClickOnFirstHighlightedObject(),
                TutorialAnimationType.LookAndPointAtFirstHighlightedObject => new LookAndPointAtFirstHighlightedObject
                {
                    rotationOffset = tutorialAnimationSettings.rotationOffset
                },
                _ => null
            };

            if (animation != null)
            {
                animation.tutorialObject = tutorialAnimationSettings.tutorialObject;
                animation.screenPositionOffset = tutorialAnimationSettings.screenPositionOffset;
                animation.duration = tutorialAnimationSettings.duration;
                animation.isLoop = tutorialAnimationSettings.isLoop;
            }

            return animation;
        }

        private HighlightSelector CreateHighlightSelector()
        {
            return highlightSelectorType switch
            {
                HighlightSelectorType.TwoRandomMatchableElements => new TwoRandomMatchableElements_Highlight(),
                HighlightSelectorType.TwoRandomSquareMatchableElements => new TwoRandomSquareMatchableElement_Highlight(),
                HighlightSelectorType.Bomb => new Bomb_Highlight(),
                HighlightSelectorType.Rocket => new Rocket_Highlight(),
                HighlightSelectorType.DiscoBall => new DiscoBall_Highlight(),
                HighlightSelectorType.ActionButton => new ActionButton_Highlight
                {
                    actionName = highlightSelectorSettings.actionName
                },
                HighlightSelectorType.SelectedTags => new SelectedTags_Highlight
                {
                    selectedTags = highlightSelectorSettings.selectedTags != null
                        ? new List<string>(highlightSelectorSettings.selectedTags)
                        : new List<string>()
                },
                _ => null
            };
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (tutorialAnimationSettings == null)
                tutorialAnimationSettings = new TutorialAnimationSettings();
            if (highlightSelectorSettings == null)
                highlightSelectorSettings = new HighlightSelectorSettings();

            RebuildRuntimeReferences();
        }
#endif
    }

    public enum TutorialAnimationType
    {
        None,
        MoveBetweenTwoPoint,
        ClickOnFirstHighlightedObject,
        LookAndPointAtFirstHighlightedObject
    }

    public enum HighlightSelectorType
    {
        None,
        TwoRandomMatchableElements,
        TwoRandomSquareMatchableElements,
        Bomb,
        Rocket,
        DiscoBall,
        ActionButton,
        SelectedTags
    }

    [Serializable]
    public class TutorialAnimationSettings
    {
        public RectTransform tutorialObject;
        public Vector2 screenPositionOffset;
        public float duration = 1f;
        public bool isLoop = true;
        public float rotationOffset = -90f;
    }

    [Serializable]
    public class HighlightSelectorSettings
    {
        public string actionName;
        public List<string> selectedTags = new List<string>();
    }
}

