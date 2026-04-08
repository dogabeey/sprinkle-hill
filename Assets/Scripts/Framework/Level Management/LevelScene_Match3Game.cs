using Sirenix.OdinInspector;
using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
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

            public float nextStageOutDuration = 0.2f;
            public float nextStageInDuration = 0.35f;
            public Vector3 nextStageInFromScale = new Vector3(0.88f, 0.88f, 1f);
        }

        [HideInInspector] public List<Objective> objectives = new List<Objective>();
        [HideInInspector] public ElementData targetElement;
        [HideInInspector] public int timer;
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

        public StageTransitionAnimationSettings stageTransitionAnimation = new StageTransitionAnimationSettings();

        private int currentLevelEditorIndex;
        private bool isSwitchingStage;

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
        }
        private void OnDisable()
        {
            EventManager.StopListening(GameEvent.OBJECTIVE_COMPLETED, OnObjectiveCompleted);
        }
        private void OnObjectiveCompleted(EventParam param)
        {
            if (isSwitchingStage)
                return;

            if (ObjectiveManager.Instance.activeObjectives != null && ObjectiveManager.Instance.activeObjectives.Count > 0
                && ObjectiveManager.Instance.activeObjectives.TrueForAll(o => o.isCompleted))
            {
                StartCoroutine(HandleStageCompleted());
            }
        }

        public IEnumerator TimerCoroutine()
        {
            EventManager.TriggerEvent(GameEvent.TIMER_PASSED);

            while (!isEnded)
            {
                yield return new WaitForSeconds(1f);

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

        private IEnumerator HandleStageCompleted()
        {
            isSwitchingStage = true;
            isPaused = true;

            if (GameManager.Instance != null && GameManager.Instance.winParticle != null)
                GameManager.Instance.winParticle.Play();

            yield return PlayRemainingElementsWinAnimation();

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
            CompleteLevel();
        }
        private IEnumerator SwitchToCurrentStage()
        {
            if (grid != null)
            {
                Transform root = grid.transform;
                root.DOKill();
                yield return root.DOScale(0.95f, stageTransitionAnimation.nextStageOutDuration).SetEase(Ease.InQuad).WaitForCompletion();
            }

            ApplyLevelSettings(true);

            if (grid != null)
            {
                Transform root = grid.transform;
                root.DOKill();
                root.localScale = stageTransitionAnimation.nextStageInFromScale;
                yield return root.DOScale(Vector3.one, stageTransitionAnimation.nextStageInDuration).SetEase(Ease.OutBack).WaitForCompletion();
            }
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

            objectives = CloneObjectives(currentEditor.objectives);
            targetElement = currentEditor.targetElement;
            timer = currentEditor.timer;

            if (grid == null)
            {
                return;
            }

            ObjectiveManager.Instance.activeObjectives = objectives;
            ObjectiveManager.Instance.InitializeObjectives();
            EventManager.TriggerEvent(GameEvent.OBJECTIVES_INITIALIZED);

            if (rebuildGrid)
            {
                grid.RebuildWithSettings(currentEditor.levelCreationMode, currentEditor, currentEditor.proceduralGeneration, currentEditor.gridSize);
            }
            else
            {
                grid.ConfigureLevelSettings(currentEditor.levelCreationMode, currentEditor, currentEditor.proceduralGeneration, currentEditor.gridSize);
            }
        }

        private LevelEditor GetCurrentLevelEditor()
        {
            if (levelEditors == null || levelEditors.Count == 0)
                return null;

            if (currentLevelEditorIndex < 0 || currentLevelEditorIndex >= levelEditors.Count)
                return null;

            return levelEditors[currentLevelEditorIndex];
        }

        private static List<Objective> CloneObjectives(List<Objective> source)
        {
            List<Objective> cloned = new List<Objective>();
            if (source == null)
                return cloned;

            for (int i = 0; i < source.Count; i++)
            {
                Objective objective = source[i];
                if (objective == null)
                    continue;

                cloned.Add(new Objective
                {
                    objectiveType = objective.objectiveType,
                    scriptableObjectParameter = objective.scriptableObjectParameter,
                    requiredCount = objective.requiredCount,
                    isCompleted = false,
                });
            }

            return cloned;
        }
    }
}