using Game;
using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ObjectiveManager : SerializedMonoBehaviour
{
    public List<Objective> activeObjectives;

    private void OnEnable()
    {
        foreach (Objective objective in activeObjectives)
        {
            EventManager.StartListening(objective.objectiveType.completionEvent, (EventParam param) =>
            {
                if (param.paramScriptable == objective.scriptableObjectParameter)
                {
                    objective.requiredCount--;
                    if (objective.requiredCount <= 0)
                    {
                        // Objective completed, trigger event or call method here
                        Debug.Log("Objective completed: " + objective.objectiveType.completionEvent);
                    }
                }
            });
        }
    }
    private void OnDisable()
    {
        
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
}