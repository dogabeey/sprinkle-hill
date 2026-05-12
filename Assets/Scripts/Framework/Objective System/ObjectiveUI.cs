using Game;
using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


public class UpperPanelUI : UIElement
{
    public enum TimerType
    {
        Countdown,
        Stopwatch
    }


    public ObjectiveManager objectiveManager;
    [AssetsOnly]
    public ObjectiveUINode objectiveNodePrefab;
    public CanvasGroup objectivesContainer;
    public Image targetIcon;
    public Image targetIconPlaceholder;
    public TMP_Text timerHeaderText;
    public TMP_Text timerText;
    [Header("Timer Settings")]
    public TimerType timerType = TimerType.Countdown;
    public string timerFormat = "{0:D2}:{1:D2}";

    private List<ObjectiveUINode> objectiveNodes = new List<ObjectiveUINode>();

    protected override void OnEnable()
    {
        base.OnEnable();
        EventManager.StartListening(GameEvent.OBJECTIVES_INITIALIZED, OnObjectivesInitialized);
        InstantiateObjectiveNodes();
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        EventManager.StopListening(GameEvent.OBJECTIVES_INITIALIZED, OnObjectivesInitialized);
    }

    public override void InitUI()
    {
        InstantiateObjectiveNodes();
        StartCoroutine(SetObjectiveTargetCoroutine());
        StartCoroutine(UpdateTimerCoroutine());
        Debug.Log("UpperPanelUI: InitUI called, timer coroutine started.");
    }
    public override void DrawUI()
    {
        EnsureObjectiveNodesSynced();
        UpdateObjectiveNodes();
    }

    private void EnsureObjectiveNodesSynced()
    {
        int activeCount = 0;
        if (objectiveManager != null && objectiveManager.activeObjectives != null)
        {
            for (int i = 0; i < objectiveManager.activeObjectives.Count; i++)
            {
                Objective objective = objectiveManager.activeObjectives[i];
                if (objective != null && !objective.tiedToLockedArea)
                    activeCount++;
            }
        }

        if (objectiveNodes.Count != activeCount)
        {
            InstantiateObjectiveNodes();
        }
    }

    private void InstantiateObjectiveNodes()
    {
        objectiveNodes.ForEach(node => Destroy(node.gameObject));
        objectiveNodes.Clear();

        if (objectiveManager == null || objectiveManager.activeObjectives == null)
            return;

        objectiveManager.activeObjectives.ForEach(objective => {
            if (objective == null || objective.tiedToLockedArea)
                return;

            ObjectiveUINode node = Instantiate(objectiveNodePrefab, transform);
            node.Initialize(objective);
            objectiveNodes.Add(node);
        });

        UpdateObjectivesContainerVisibility();
    }
    private void UpdateObjectiveNodes()
    {
        UpdateObjectivesContainerVisibility();

        objectiveNodes.ForEach(node =>
        {
            Objective objective = node.referenceObjective;
            int currentCount = objectiveManager.GetCurrentCount(objective);
            node.UpdateNode(currentCount);
        });
    }

    private void UpdateObjectivesContainerVisibility()
    {
        bool hasVisibleObjectives = false;
        if (objectiveManager != null && objectiveManager.activeObjectives != null)
        {
            for (int i = 0; i < objectiveManager.activeObjectives.Count; i++)
            {
                Objective objective = objectiveManager.activeObjectives[i];
                if (objective != null && !objective.tiedToLockedArea)
                {
                    hasVisibleObjectives = true;
                    break;
                }
            }
        }

        if (!hasVisibleObjectives)
        {
            objectivesContainer.alpha = 0f;
        }
        else
        {
            objectivesContainer.alpha = 1f;
        }
    }

    public IEnumerator UpdateTimerCoroutine()
    {
        Debug.Log("UpperPanelUI: UpdateTimerCoroutine started, waiting for CurrentLevel...");
        yield return new WaitUntil(() => GameManager.Instance.CurrentLevel != null);
        LevelScene_Match3Game levelScene = GameManager.Instance.CurrentLevel as LevelScene_Match3Game;

        if (levelScene == null)
        {
            Debug.LogError("UpperPanelUI: CurrentLevel is not a LevelScene_Match3Game. Timer/moves display will not work.");
            yield break;
        }

        Debug.Log($"UpperPanelUI: Level scene found. Limit type: {levelScene.levelLimitType}, Initial moves: {levelScene.moves}, Initial timer: {levelScene.timer}");

        while (true)
        {
            if (levelScene.levelLimitType == LevelEditor.LevelLimitType.Moves)
            {
                timerHeaderText.text = "Move";
                timerText.enableAutoSizing = false;
                timerText.text = Mathf.Max(0, levelScene.moves).ToString();
            }
            else
            {
                timerHeaderText.text = "Time";

                int timer = levelScene.timer;
                if (timer == -1)
                {
                    timerText.text = "∞";
                    timerText.enableAutoSizing = true;
                }
                else
                {
                    timerText.enableAutoSizing = false;
                    timerText.text = Mathf.Max(0, timer).ToString();
                }
            }

            if (levelScene.isEnded)
                yield break;

            yield return null;
        }
    }

    private IEnumerator SetObjectiveTargetCoroutine()
    {
        yield return new WaitUntil(() => GameManager.Instance.CurrentLevel != null);
        LevelScene_Match3Game levelScene = GameManager.Instance.CurrentLevel as LevelScene_Match3Game;
        
        while (!levelScene.isEnded)
        {
            if(targetIcon != null && levelScene.targetElement != null)
            {
                targetIconPlaceholder.enabled = false;
                targetIcon.enabled = true;
                targetIcon.sprite = levelScene.targetElement.displayIcon;
            }
            else
            {
                targetIconPlaceholder.enabled = true;
                targetIcon.enabled = false;
            }
                yield return null;
        }
    }

    private string FormatTime(float elapsedTime)
    {
        TimeSpan timeSpan = TimeSpan.FromSeconds(elapsedTime);
        switch (timerType)
        {
            case TimerType.Countdown:
                return string.Format(timerFormat, elapsedTime);
            case TimerType.Stopwatch:
                return string.Format(timerFormat, timeSpan.Minutes, timeSpan.Seconds);
            default:
                return string.Format(timerFormat, timeSpan.Minutes, timeSpan.Seconds);
        }
    }

    private void OnObjectivesInitialized(EventParam param)
    {
        InstantiateObjectiveNodes();
    }

}
