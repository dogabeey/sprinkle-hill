using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using UnityEngine; using Game.EventManagement;
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
        [OnValueChanged(nameof(InitTutorialAnimation))] public TutorialAnimationType tutorialAnimationType;
        public TutorialAnimationSettings tutorialAnimationSettings = new TutorialAnimationSettings();

        private void InitTutorialAnimation()
        {
            Debug.Log($"InitTutorialAnimation: {tutorialAnimationType}");
            tutorialAnimationSettings = new TutorialAnimationSettings
            {
                tutorialAnimationType = tutorialAnimationType
            };
        }
        [LabelText("Highlight Selectors")]
        public List<HighlightSelectorSettings> highlightSelectors = new List<HighlightSelectorSettings>();

        // Kept for existing TutorialStep assets. New steps should use highlightSelectors.
        [HideInInspector] public HighlightSelectorType highlightSelectorType;
        [HideInInspector] public HighlightSelectorSettings highlightSelectorSettings = new HighlightSelectorSettings();
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
        [NonSerialized] private List<HighlightSelector> runtimeHighlightSelectors;

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

        public IReadOnlyList<HighlightSelector> GetHighlightSelectors()
        {
            if (runtimeHighlightSelectors == null)
                runtimeHighlightSelectors = CreateHighlightSelectors();

            return runtimeHighlightSelectors;
        }

        [Obsolete("Use GetHighlightSelectors or GetHighlightedObjects instead.")]
        public HighlightSelector GetHighlightSelector()
        {
            IReadOnlyList<HighlightSelector> selectors = GetHighlightSelectors();
            return selectors.Count > 0 ? selectors[0] : null;
        }

        public GameObject[] GetHighlightedObjects()
        {
            List<GameObject> highlightedObjects = new List<GameObject>();
            HashSet<GameObject> uniqueObjects = new HashSet<GameObject>();
            IReadOnlyList<HighlightSelector> selectors = GetHighlightSelectors();

            for (int selectorIndex = 0; selectorIndex < selectors.Count; selectorIndex++)
            {
                GameObject[] selectorObjects = selectors[selectorIndex]?.HighlightedObjects;
                if (selectorObjects == null)
                    continue;

                for (int objectIndex = 0; objectIndex < selectorObjects.Length; objectIndex++)
                {
                    GameObject highlightedObject = selectorObjects[objectIndex];
                    if (highlightedObject != null && uniqueObjects.Add(highlightedObject))
                        highlightedObjects.Add(highlightedObject);
                }
            }

            return highlightedObjects.ToArray();
        }

        public void RebuildRuntimeReferences()
        {
            runtimeTutorialAnimation = CreateTutorialAnimation();
            runtimeHighlightSelectors = CreateHighlightSelectors();
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
                TutorialAnimationType.MoveBetweenTwoCoordinates => new MoveBetweenTwoCoordinates()
                {
                    startCoordinate = tutorialAnimationSettings.startCoordinate,
                    endCoordinate = tutorialAnimationSettings.endCoordinate
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

        private List<HighlightSelector> CreateHighlightSelectors()
        {
            List<HighlightSelector> selectors = new List<HighlightSelector>();
            if (highlightSelectors != null)
            {
                for (int i = 0; i < highlightSelectors.Count; i++)
                {
                    HighlightSelector selector = CreateHighlightSelector(highlightSelectors[i]);
                    if (selector != null)
                        selectors.Add(selector);
                }
            }

            // Existing assets have only the legacy selector fields until they are opened and migrated in the editor.
            if (selectors.Count == 0 && highlightSelectorType != HighlightSelectorType.None)
            {
                HighlightSelector selector = CreateHighlightSelector(highlightSelectorSettings);
                if (selector != null)
                    selectors.Add(selector);
            }

            return selectors;
        }

        private HighlightSelector CreateHighlightSelector(HighlightSelectorSettings settings)
        {
            if (settings == null)
                return null;

            return settings.highlightSelectorType switch
            {
                HighlightSelectorType.TwoRandomMatchableElements => new TwoRandomMatchableElements_Highlight(),
                HighlightSelectorType.TwoRandomSquareMatchableElements => new TwoRandomSquareMatchableElement_Highlight(),
                HighlightSelectorType.Bomb => new Bomb_Highlight(),
                HighlightSelectorType.Rocket => new Rocket_Highlight(),
                HighlightSelectorType.DiscoBall => new DiscoBall_Highlight(),
                HighlightSelectorType.ActionButton => new ActionButton_Highlight
                {
                    actionName = settings.actionName
                },
                HighlightSelectorType.SelectedTags => new SelectedTags_Highlight
                {
                    selectedTags = settings.selectedTags != null
                        ? new List<string>(settings.selectedTags)
                        : new List<string>()
                },
                HighlightSelectorType.SelectedGridCoordinates => new SelectedGridCoordinates_Highlight
                {
                    selectedCoordinates = settings.selectedCoordinates != null
                        ? new List<Vector2Int>(settings.selectedCoordinates)
                        : new List<Vector2Int>()
                },
                HighlightSelectorType.AllWaferCells => new AllWaferElements_Highlight(),
                HighlightSelectorType.AllGlassCells => new AllGlassElements_Highlight(),
                _ => null
            };
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (tutorialAnimationSettings == null)
                tutorialAnimationSettings = new TutorialAnimationSettings();

            if (highlightSelectors == null)
                highlightSelectors = new List<HighlightSelectorSettings>();

            if (highlightSelectors.Count == 0 && highlightSelectorType != HighlightSelectorType.None)
            {
                highlightSelectorSettings ??= new HighlightSelectorSettings();
                highlightSelectorSettings.highlightSelectorType = highlightSelectorType;
                highlightSelectors.Add(highlightSelectorSettings);
                highlightSelectorType = HighlightSelectorType.None;
            }

            RebuildRuntimeReferences();
        }
#endif
    }

    public enum TutorialAnimationType
    {
        None,
        MoveBetweenTwoPoint,
        ClickOnFirstHighlightedObject,
        LookAndPointAtFirstHighlightedObject,
        MoveBetweenTwoCoordinates,
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
        SelectedTags,
        SelectedGridCoordinates,
        AllWaferCells,
        AllGlassCells,
    }

    [Serializable]
    public class TutorialAnimationSettings
    {
        [HideInInspector] public TutorialAnimationType tutorialAnimationType;
        public RectTransform tutorialObject;
        [ShowIf(nameof(IsScreenPositionOffsetRequired))] public Vector2 screenPositionOffset;
        public float duration = 1f;
        public bool isLoop = true;
        public float rotationOffset = -90f;
        [ShowIf(nameof(AreStartAndEndCoordinatesRequired))] public Vector2Int startCoordinate;
        [ShowIf(nameof(AreStartAndEndCoordinatesRequired))] public Vector2Int endCoordinate;

        private bool IsScreenPositionOffsetRequired()
        {
            return tutorialAnimationType == TutorialAnimationType.LookAndPointAtFirstHighlightedObject;
        }
        private bool AreStartAndEndCoordinatesRequired()
        {
            return tutorialAnimationType == TutorialAnimationType.MoveBetweenTwoCoordinates;
        }
    }

    [Serializable]
    public class HighlightSelectorSettings
    {
        [LabelText("Selector")] public HighlightSelectorType highlightSelectorType;
        [ShowIf(nameof(IsActionNameRequired))] public string actionName;
        [ShowIf(nameof(AreSelectedTagsRequired))] public List<string> selectedTags = new List<string>();
        [ShowIf(nameof(AreSelectedCoordinatesRequired))] public List<Vector2Int> selectedCoordinates = new List<Vector2Int>();

        public bool IsActionNameRequired()
        {
            return highlightSelectorType == HighlightSelectorType.ActionButton;
        }
        public bool AreSelectedTagsRequired()
        {
            return highlightSelectorType == HighlightSelectorType.SelectedTags;
        }
        public bool AreSelectedCoordinatesRequired()
        {
            return highlightSelectorType == HighlightSelectorType.SelectedGridCoordinates;
        }
    }
}

