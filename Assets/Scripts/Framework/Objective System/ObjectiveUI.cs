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
    public ObjectiveManager objectiveManager;
    [AssetsOnly]
    public ObjectiveUINode objectiveNodePrefab;
    public Image targetIcon;
    public TMP_Text timerText;

    private List<ObjectiveUINode> objectiveNodes = new List<ObjectiveUINode>();

    public override void InitUI()
    {
        InstantiateObjectiveNodes();
        StartCoroutine(TimerCoroutine());
        StartCoroutine(SetObjectiveTargetCoroutine());
    }
    public override void DrawUI()
    {
        UpdateObjectiveNodes();
    }

    private void InstantiateObjectiveNodes()
    {
        objectiveManager.activeObjectives.ForEach(objective => {
            ObjectiveUINode node = Instantiate(objectiveNodePrefab, transform);
            node.Initialize(objective);
            objectiveNodes.Add(node);
        });
    }
    private void UpdateObjectiveNodes()
    {
        objectiveNodes.ForEach(node => {
            Objective objective = node.referenceObjective;
            int currentCount = objectiveManager.GetCurrentCount(objective);
            node.UpdateNode(currentCount);
        });
    }

    private IEnumerator TimerCoroutine()
    {
        yield return new WaitUntil(() => GameManager.Instance.CurrentLevel != null);
        LevelScene_Match3Game levelScene = GameManager.Instance.CurrentLevel as LevelScene_Match3Game;
        int timer = levelScene.timer;


        float elapsedTime = timer;
        while (!levelScene.isEnded)
        {
            elapsedTime -= Time.deltaTime;
            timerText.text = elapsedTime > 0 ? FormatTime(elapsedTime) : "0:00";
            yield return null;
        }
    }

    private IEnumerator SetObjectiveTargetCoroutine()
    {
        yield return new WaitUntil(() => GameManager.Instance.CurrentLevel != null);
        LevelScene_Match3Game levelScene = GameManager.Instance.CurrentLevel as LevelScene_Match3Game;
        
        while (!levelScene.isEnded)
        {
            targetIcon.sprite = levelScene.targetElement.displayIcon;
            yield return null;
        }
    }

    private string FormatTime(float elapsedTime)
    {
        TimeSpan timeSpan = TimeSpan.FromSeconds(elapsedTime);
        return string.Format("{0:D2}:{1:D2}", timeSpan.Minutes, timeSpan.Seconds);
    }

}
