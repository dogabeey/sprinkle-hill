using Game;
using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ObjectiveManager : SingletonComponent<ObjectiveManager>
{
    public List<Objective> activeObjectives;

    public void InitializeObjectives()
    {
        ClearObjectiveListeners();
        foreach (Objective objective in activeObjectives)
        {
            EventManager.StartListening(objective.objectiveType.completionEvent, (EventParam param) =>
            {
                if (objective.scriptableObjectParameter == null || param.paramScriptable == objective.scriptableObjectParameter)
                {
                    objective.requiredCount--;
                    if (objective.requiredCount <= 0)
                    {
                        // Objective completed, trigger event or call method here
                        Debug.Log("Objective completed: " + objective.objectiveType.completionEvent);
                        objective.isCompleted = true;
                        EventManager.TriggerEvent(GameEvent.OBJECTIVE_COMPLETED, new EventParam(
                            paramScriptable: objective.objectiveType
                        ));
                    }
                }
            });
        }

    }
    public void ClearObjectiveListeners()
    {
        foreach (Objective objective in activeObjectives)
        {
            EventManager.StopListening(objective.objectiveType.completionEvent, null);
        }
    }

    private void OnEnable()
    {
        EventManager.StartListening(GameEvent.LEVEL_STARTED, OnLevelStarted);
        EventManager.StartListening(GameEvent.LEVEL_COMPLETED, OnLevelCompleted);
        EventManager.StartListening(GameEvent.LEVEL_FAILED, OnLevelCompleted);
    }
    private void OnDisable()
    {
        EventManager.StopListening(GameEvent.LEVEL_STARTED, OnLevelStarted);
        EventManager.StopListening(GameEvent.LEVEL_COMPLETED, OnLevelCompleted);
        EventManager.StopListening(GameEvent.LEVEL_FAILED, OnLevelCompleted);
    }

    private void OnLevelStarted(EventParam param)
    {
    }
    private void OnLevelCompleted(EventParam param)
    {
        activeObjectives.Clear();
        ClearObjectiveListeners();
    }
    internal int GetCurrentCount(Objective objective)
    {
        return objective.requiredCount;
    }
}

[System.Serializable]
public class Objective
{
    [Tooltip("The type of this objective. Created via scriptable object, it defines the event that will be listened to for this objective and the parameter type of that event.")]
    public ObjectiveType objectiveType;
    [Tooltip("The scriptable object parameter associated with this objective's event. Every time the event specified in objective type is sent with this specific scriptable object, the required objective count will decreased.")]
    public VisualizableScriptableObject scriptableObjectParameter;
    public int requiredCount;
    public bool isCompleted;
}
