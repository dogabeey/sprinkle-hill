using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;

namespace Game
{
    [CreateAssetMenu(menuName = "Tutorial/Tutorial Step...", fileName = "TutorialStep")]
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
        [FoldoutGroup("Custom Events")]
        public UnityEvent onStart;
        [FoldoutGroup("Custom Events")]
        public UnityEvent onComplete;[SerializeReference]
        [GUIColor(nameof(GetNextStepColor))]
        public TutorialStep nextStep;
        public bool advancedMode;
        [HideInInspector]
        public int serializationDepth;
        [HideInInspector]
        public bool isStarted;
        [HideInInspector]
        public bool isCompleted;

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
    }
}

