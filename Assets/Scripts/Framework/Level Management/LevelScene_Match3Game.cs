using Sirenix.OdinInspector;
using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game
{
    public class LevelScene_Match3Game : LevelScene
    {
        [Serializable]
        public class StageTransitionAnimationSettings
        {

            public float elementWinAnimDuration = 0.3f;
            public float elementWinAnimStagger = 0.01f;
            public Vector3 elementWinPunchScale = new Vector3(0.25f, 0.25f, 0f);
            public float elementWinMoveUp = 0.35f;

            public float previousStageElementOutDuration = 0.2f;
            public float previousStageElementOutStagger = 0.008f;
            public float previousStageElementOutRotateZ = 120f;
            public Vector3 previousStageElementOutScale = new Vector3(0.05f, 0.05f, 0.05f);

            public float newStageElementDropFromY = 2.5f;
            public float newStageElementDropDuration = 0.28f;
            public float newStageElementDropStagger = 0.006f;

            public float stagePopupInDuration = 0.2f;
            public float stagePopupHoldDuration = 0.45f;
            public float stagePopupOutDuration = 0.2f;
        }

        [HideInInspector] public List<Objective> objectives = new List<Objective>();
        [HideInInspector] public ElementData targetElement;
        [HideInInspector] public LevelEditor.LevelLimitType levelLimitType;
        [HideInInspector] public int timer;
        [HideInInspector] public int moves;
        [FoldoutGroup("Level Settings")]
        public Grid3D grid;
        [FoldoutGroup("Level Settings")]
        public List<LevelEditor> levelEditors = new List<LevelEditor>();
        [FoldoutGroup("Power Up Settings")]
        [SerializeField] private bool allowDiscoBallCreation = true;
        [FoldoutGroup("Power Up Settings")]
        [SerializeField] private bool allowRocketCreation = true;
        [FoldoutGroup("Power Up Settings")]
        [SerializeField] private bool allowBombCreation = true;

        [FoldoutGroup("Power Up Settings")]
        public int sparklingPowerAfterXCombo = 3;
        [FoldoutGroup("Power Up Settings"), Range(0f, 1f)]
        public float sparklingAppearChance = 0.3f;
        [FoldoutGroup("Power Up Settings")]
        public ElementData bombElementData;
        [FoldoutGroup("Power Up Settings")]
        public ElementData rocketElementData;
        [FoldoutGroup("Power Up Settings")]
        public ElementData discoBallElementData;

        [FoldoutGroup("Stage Transition")]
        public TMP_Text stageCompletePopupTextPrefab;

        public StageTransitionAnimationSettings stageTransitionAnimation = new StageTransitionAnimationSettings();

        private int currentLevelEditorIndex;
        private bool isSwitchingStage;
        private bool stageCompletionPending;
        private bool stageCompletionGridStable;
        private bool stageCompletionRoutineRunning;

        private static readonly string[] StageCompleteMessages =
        {
            "Great!",
            "Awesome!",
            "Nice!",
            "Perfect!",
            "Excellent!",
            "Well Done!"
        };

        public int CurrentStageIndex => currentLevelEditorIndex;
        public bool AllowDiscoBallCreation => allowDiscoBallCreation;
        public bool AllowRocketCreation => allowRocketCreation;
        public bool AllowBombCreation => allowBombCreation;

        protected override void Awake()
        {
            currentLevelEditorIndex = 0;
            ApplyLevelSettings(false);
            base.Awake();
            StartCoroutine(TimerCoroutine());
        }
        private void OnEnable()
        {
            EventManager.StartListening(GameEvent.OBJECTIVE_COMPLETED, OnObjectiveCompleted);
            EventManager.StartListening(GameEvent.GRID_STABLE, OnGridStable);
            EventManager.StartListening(GameEvent.ELEMENTS_SWAPPED, OnElementsSwapped);
            SetLevelWinEventListener();
        }
        private void OnDisable()
        {
            EventManager.StopListening(GameEvent.OBJECTIVE_COMPLETED, OnObjectiveCompleted);
            EventManager.StopListening(GameEvent.GRID_STABLE, OnGridStable);
            EventManager.StopListening(GameEvent.ELEMENTS_SWAPPED, OnElementsSwapped);
        }
        private void OnObjectiveCompleted(EventParam param)
        {
            if (isSwitchingStage)
                return;

            if (ObjectiveManager.Instance.activeObjectives != null && ObjectiveManager.Instance.activeObjectives.Count > 0
                && ObjectiveManager.Instance.activeObjectives.TrueForAll(o => o.isCompleted))
            {
                RequestStageCompletion();
            }
        }

        private void RequestStageCompletion()
        {
            stageCompletionPending = true;
            stageCompletionGridStable = false;
            isPaused = true;

            if (!stageCompletionRoutineRunning)
                StartCoroutine(BeginStageCompletionWhenReady());
        }

        private void OnGridStable(EventParam param)
        {
            if (!stageCompletionPending)
                return;

            stageCompletionGridStable = true;
        }

        private IEnumerator BeginStageCompletionWhenReady()
        {
            stageCompletionRoutineRunning = true;

            float waitTimeout = 5f;
            while (!isEnded && !stageCompletionGridStable && waitTimeout > 0f)
            {
                waitTimeout -= Time.deltaTime;
                yield return null;
            }

            if (stageCompletionPending && !isSwitchingStage && !isEnded)
            {
                stageCompletionPending = false;
                StartCoroutine(HandleStageCompleted());
            }

            stageCompletionRoutineRunning = false;
        }

        public IEnumerator TimerCoroutine()
        {
            EventManager.TriggerEvent(GameEvent.TIMER_PASSED);

            while (!isEnded)
            {
                yield return new WaitForSeconds(1f);

                if (levelLimitType != LevelEditor.LevelLimitType.Timer)
                {
                    EventManager.TriggerEvent(GameEvent.TIMER_PASSED);
                    continue;
                }

                if (timer == -1)
                {
                    EventManager.TriggerEvent(GameEvent.TIMER_PASSED);
                    continue;
                }

                if (!isPaused && timer >= 0)
                {
                    timer--;
                    EventManager.TriggerEvent(GameEvent.TIMER_PASSED);
                }

                if (timer <= 0)
                {
                    timer = 0;
                    EventManager.TriggerEvent(GameEvent.TIMER_PASSED);
                    EventManager.TriggerEvent(GameEvent.TIMER_EXPIRED);
                    FailLevel("You are out of time.\n\nDon't worry, you'll get them next time.");
                }
            }
        }

        private void OnElementsSwapped(EventParam param)
        {
            if (levelLimitType != LevelEditor.LevelLimitType.Moves)
                return;

            if (isPaused || isEnded)
                return;

            if (moves <= 0)
                return;

            moves--;
            EventManager.TriggerEvent(GameEvent.TIMER_PASSED);

            if (moves <= 0)
            {
                moves = 0;
                EventManager.TriggerEvent(GameEvent.TIMER_EXPIRED);
                FailLevel("You are out of moves.\n\nDon't worry, you'll get them next time.");
            }
        }

        private IEnumerator HandleStageCompleted()
        {
            isSwitchingStage = true;
            isPaused = true;

            yield return PlayRemainingElementsWinAnimation();
            yield return PlayStageCompletePopupText();

            if (currentLevelEditorIndex + 1 < levelEditors.Count)
            {
                RemoveLevelWinEventListener();
                currentLevelEditorIndex++;
                SetLevelWinEventListener();
                yield return SwitchToCurrentStage();
                isPaused = false;
                isSwitchingStage = false;
                yield break;
            }

            isPaused = false;
            isSwitchingStage = false;
            CompleteLevel();
        }

        private IEnumerator PlayStageCompletePopupText()
        {
            if (stageCompletePopupTextPrefab == null || StageCompleteMessages.Length == 0)
                yield break;

            Canvas canvas = GameManager.Instance.mainCanvas;
            TMP_Text popup = canvas != null
                ? Instantiate(stageCompletePopupTextPrefab, canvas.transform)
                : Instantiate(stageCompletePopupTextPrefab);

            popup.text = StageCompleteMessages[UnityEngine.Random.Range(0, StageCompleteMessages.Length)];

            Transform popupTransform = popup.transform;
            popupTransform.localScale = Vector3.zero;

            CanvasGroup canvasGroup = popup.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = popup.gameObject.AddComponent<CanvasGroup>();
            canvasGroup.alpha = 0f;

            float inDuration = Mathf.Max(0.01f, stageTransitionAnimation.stagePopupInDuration);
            float holdDuration = Mathf.Max(0f, stageTransitionAnimation.stagePopupHoldDuration);
            float outDuration = Mathf.Max(0.01f, stageTransitionAnimation.stagePopupOutDuration);

            Sequence sequence = DOTween.Sequence();
            sequence.Append(canvasGroup.DOFade(1f, inDuration));
            sequence.Join(popupTransform.DOScale(1f, inDuration).SetEase(Ease.OutBack));
            sequence.AppendInterval(holdDuration);
            sequence.Append(canvasGroup.DOFade(0f, outDuration));
            sequence.Join(popupTransform.DOScale(0.85f, outDuration).SetEase(Ease.InBack));

            yield return sequence.WaitForCompletion();

            if (popup != null)
                Destroy(popup.gameObject);
        }
        private void RemoveLevelWinEventListener()
        {
            EventManager.StopListening(GetCurrentLevelEditor().specialWinEvent, OnWinEventFire);
        }
        private void SetLevelWinEventListener()
        {
            if(GetCurrentLevelEditor() == null)
            {
                return;
            }
            EventManager.StartListening(GetCurrentLevelEditor().specialWinEvent, OnWinEventFire);
        }
        private void OnWinEventFire(EventParam param)
        {
            RequestStageCompletion();
        }
        private IEnumerator SwitchToCurrentStage()
        {
            yield return PlayPreviousStageElementsOutAnimation();

            ApplyLevelSettings(true);

            yield return PlayNewStageElementsDropAnimation();
        }

        private IEnumerator PlayPreviousStageElementsOutAnimation()
        {
            if (!(grid is Match3Grid match3Grid))
                yield break;

            List<GridElement> elements = match3Grid.GetAllActiveElements();
            if (elements.Count == 0)
                yield break;

            float duration = Mathf.Max(0.05f, stageTransitionAnimation.previousStageElementOutDuration);
            Sequence sequence = DOTween.Sequence();

            for (int i = 0; i < elements.Count; i++)
            {
                GridElement element = elements[i];
                if (element == null)
                    continue;

                Transform t = element.transform;
                t.DOKill();

                Sequence perElement = DOTween.Sequence();
                perElement.Join(t.DOScale(stageTransitionAnimation.previousStageElementOutScale, duration).SetEase(Ease.InBack));
                perElement.Join(t.DOLocalRotate(new Vector3(0f, 0f, stageTransitionAnimation.previousStageElementOutRotateZ), duration, RotateMode.FastBeyond360)
                    .SetEase(Ease.InQuad)
                    .SetRelative());

                sequence.Insert(i * stageTransitionAnimation.previousStageElementOutStagger, perElement);
            }

            yield return sequence.WaitForCompletion();
        }

        private IEnumerator PlayNewStageElementsDropAnimation()
        {
            if (!(grid is Match3Grid match3Grid))
                yield break;

            List<GridElement> elements = match3Grid.GetAllActiveElements();
            if (elements.Count == 0)
                yield break;

            float duration = Mathf.Max(0.05f, stageTransitionAnimation.newStageElementDropDuration);
            Sequence sequence = DOTween.Sequence();

            for (int i = 0; i < elements.Count; i++)
            {
                GridElement element = elements[i];
                if (element == null)
                    continue;

                Transform t = element.transform;
                t.DOKill();
                Vector3 endLocalPos = t.localPosition;
                t.localPosition = endLocalPos + Vector3.up * stageTransitionAnimation.newStageElementDropFromY;

                sequence.Insert(i * stageTransitionAnimation.newStageElementDropStagger,
                    t.DOLocalMove(endLocalPos, duration).SetEase(Ease.OutBounce));
            }

            yield return sequence.WaitForCompletion();
        }

        private IEnumerator PlayRemainingElementsWinAnimation()
        {
            if (!(grid is Match3Grid match3Grid))
                yield break;

            List<GridElement> elements = match3Grid.GetAllActiveElements();
            if (elements.Count == 0)
                yield break;

            Sequence sequence = DOTween.Sequence();
            float duration = Mathf.Max(0.05f, stageTransitionAnimation.elementWinAnimDuration);

            for (int i = 0; i < elements.Count; i++)
            {
                GridElement element = elements[i];
                if (element == null) continue;

                Transform t = element.transform;
                t.DOKill();

                Sequence perElement = DOTween.Sequence();
                perElement.Append(t.DOPunchScale(stageTransitionAnimation.elementWinPunchScale, duration, 8, 0.8f));
                perElement.Join(t.DOLocalMoveY(t.localPosition.y + stageTransitionAnimation.elementWinMoveUp, duration)
                    .SetLoops(2, LoopType.Yoyo)
                    .SetEase(Ease.InOutSine));

                sequence.Insert(i * stageTransitionAnimation.elementWinAnimStagger, perElement);
            }

            yield return sequence.WaitForCompletion();
        }

        private void ApplyLevelSettings(bool rebuildGrid)
        {
            LevelEditor currentEditor = GetCurrentLevelEditor();
            if (currentEditor == null)
            {
                Debug.LogError("[LevelScene_Match3Game] No valid LevelEditor found in levelEditors list.");
                return;
            }

            objectives = CloneObjectives(currentEditor.objectives, currentEditor.ElementPool);
            targetElement = currentEditor.targetElement;
            levelLimitType = currentEditor.levelLimitType;
            timer = currentEditor.timer;
            moves = Mathf.Max(0, currentEditor.moves);

            if (grid == null)
            {
                return;
            }

            ObjectiveManager.Instance.activeObjectives = objectives;
            ObjectiveManager.Instance.InitializeObjectives();
            EventManager.TriggerEvent(GameEvent.OBJECTIVES_INITIALIZED);

            Grid3D.ProceduralGenerationSettings proceduralSettings = CreateRuntimeProceduralSettings(currentEditor);

            if (rebuildGrid)
            {
                grid.RebuildWithSettings(currentEditor.levelCreationMode, currentEditor, proceduralSettings, currentEditor.gridSize);
            }
            else
            {
                grid.ConfigureLevelSettings(currentEditor.levelCreationMode, currentEditor, proceduralSettings, currentEditor.gridSize);
            }
        }

        private static Grid3D.ProceduralGenerationSettings CreateRuntimeProceduralSettings(LevelEditor levelEditor)
        {
            Grid3D.ProceduralGenerationSettings source = levelEditor != null ? levelEditor.proceduralGeneration : null;
            Grid3D.ProceduralGenerationSettings settings = new Grid3D.ProceduralGenerationSettings();

            if (source != null)
            {
                settings.useRandomSeed = source.useRandomSeed;
                settings.seed = source.seed;
                settings.emptyCellChance = source.emptyCellChance;
                settings.breakableWallPlacementMode = source.breakableWallPlacementMode;
                settings.triangleWidth = source.triangleWidth;
                settings.triangleDecrement = source.triangleDecrement;
                settings.rectangleStart = source.rectangleStart;
                settings.rectangleEnd = source.rectangleEnd;
                settings.hiddenBoxChance = source.hiddenBoxChance;
            }

            settings.elementPool = new List<ElementData>();

            if (levelEditor != null && levelEditor.ElementPool != null && levelEditor.ElementPool.Count > 0)
            {
                settings.elementPool.AddRange(levelEditor.ElementPool);
            }

            return settings;
        }

        private LevelEditor GetCurrentLevelEditor()
        {
            if (levelEditors == null || levelEditors.Count == 0)
                return null;

            if (currentLevelEditorIndex < 0 || currentLevelEditorIndex >= levelEditors.Count)
                return null;

            return levelEditors[currentLevelEditorIndex];
        }

        private static List<Objective> CloneObjectives(List<Objective> source, List<ElementData> possibleElementsPool)
        {
            List<Objective> cloned = new List<Objective>();
            if (source == null)
                return cloned;

            List<ElementData> validElementPool = possibleElementsPool != null
                ? possibleElementsPool.Where(element => element != null).ToList()
                : new List<ElementData>();

            for (int i = 0; i < source.Count; i++)
            {
                Objective objective = source[i];
                if (objective == null)
                    continue;

                VisualizableScriptableObject scriptableParameter = objective.scriptableObjectParameter;
                int requiredCount = objective.requiredCount;

                if (objective.isProcedurallyGenerated)
                {
                    if (validElementPool.Count > 0)
                    {
                        scriptableParameter = validElementPool[UnityEngine.Random.Range(0, validElementPool.Count)];
                    }

                    int minCount = Mathf.RoundToInt(Mathf.Min(objective.generatedCount.x, objective.generatedCount.y));
                    int maxCount = Mathf.RoundToInt(Mathf.Max(objective.generatedCount.x, objective.generatedCount.y));

                    minCount = Mathf.Max(1, minCount);
                    maxCount = Mathf.Max(minCount, maxCount);

                    requiredCount = UnityEngine.Random.Range(minCount, maxCount + 1);
                }

                cloned.Add(new Objective
                {
                    objectiveType = objective.objectiveType,
                    isProcedurallyGenerated = objective.isProcedurallyGenerated,
                    scriptableObjectParameter = scriptableParameter,
                    requiredCount = requiredCount,
                    generatedCount = objective.generatedCount,
                    isCompleted = false,
                });
            }

            return cloned;
        }
    }
}