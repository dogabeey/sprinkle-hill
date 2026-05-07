using Game;
using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Game
{
    public class ObjectiveManager : SingletonComponent<ObjectiveManager>
    {
        public List<Objective> activeObjectives;
        private readonly Dictionary<Objective, Action<EventParam>> completionListeners = new Dictionary<Objective, Action<EventParam>>();
        private readonly Dictionary<Objective, Action<EventParam>> creationCountListeners = new Dictionary<Objective, Action<EventParam>>();

        public void InitializeObjectives()
        {
            ClearObjectiveListeners();

            if (activeObjectives == null)
                return;

            foreach (Objective objective in activeObjectives)
            {
                if (objective == null || objective.objectiveType == null)
                    continue;

                objective.isCompleted = false;

                if (objective.autoCountRequiredCount)
                {
                    objective.requiredCount = 0;
                    objective.initialRequiredCount = 0;
                    objective.isCompleted = false;
                }
                else
                {
                    objective.requiredCount = Mathf.Max(0, objective.requiredCount);
                    objective.initialRequiredCount = Mathf.Max(0, objective.initialRequiredCount > 0 ? objective.initialRequiredCount : objective.requiredCount);
                    objective.isCompleted = objective.requiredCount <= 0;
                }

                Action<EventParam> completionListener = (EventParam param) =>
                {
                    if (!DoesObjectiveEventMatch(objective, param))
                        return;

                    if (objective.isCompleted || objective.requiredCount <= 0)
                        return;

                    objective.requiredCount = Mathf.Max(0, objective.requiredCount - 1);
                    EventManager.TriggerEvent(GameEvent.OBJECTIVE_PROGRESS_UPDATED);

                    if (objective.requiredCount <= 0)
                    {
                        Debug.Log("Objective completed: " + objective.objectiveType.completionEvent);
                        objective.isCompleted = true;
                        EventManager.TriggerEvent(GameEvent.OBJECTIVE_COMPLETED, new EventParam(
                            paramScriptable: objective.objectiveType
                        ));
                    }
                };

                completionListeners[objective] = completionListener;
                EventManager.StartListening(objective.objectiveType.completionEvent, completionListener);

                if (objective.autoCountRequiredCount && objective.objectiveType.creationCountEvent != GameEvent.NONE)
                {
                    Action<EventParam> creationListener = (EventParam param) =>
                    {
                        if (!DoesObjectiveEventMatch(objective, param))
                            return;

                        objective.requiredCount++;
                        objective.initialRequiredCount++;
                        objective.isCompleted = false;
                        EventManager.TriggerEvent(GameEvent.OBJECTIVE_PROGRESS_UPDATED);
                    };

                    creationCountListeners[objective] = creationListener;
                    EventManager.StartListening(objective.objectiveType.creationCountEvent, creationListener);
                }
                else
                {
                    objective.initialRequiredCount = Mathf.Max(objective.initialRequiredCount, objective.requiredCount);
                }
            }

        }

        private static bool DoesObjectiveEventMatch(Objective objective, EventParam param)
        {
            if (objective == null)
                return false;

            if (objective.scriptableObjectParameter == null)
                return true;

            if (param == null)
                return false;

            return param.paramScriptable == objective.scriptableObjectParameter;
        }

        public void ClearObjectiveListeners()
        {
            foreach (var pair in completionListeners)
            {
                Objective objective = pair.Key;
                if (objective != null && objective.objectiveType != null)
                    EventManager.StopListening(objective.objectiveType.completionEvent, pair.Value);
            }

            foreach (var pair in creationCountListeners)
            {
                Objective objective = pair.Key;
                if (objective != null && objective.objectiveType != null)
                    EventManager.StopListening(objective.objectiveType.creationCountEvent, pair.Value);
            }

            completionListeners.Clear();
            creationCountListeners.Clear();
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
            ClearObjectiveListeners();
        }

        private void OnLevelStarted(EventParam param)
        {
        }
        private void OnLevelCompleted(EventParam param)
        {
            ClearObjectiveListeners();
            activeObjectives?.Clear();
        }
        internal int GetCurrentCount(Objective objective)
        {
            return objective.requiredCount;
        }
        public float GetTotalRemainingObjectives()
        {
            return ObjectiveManager.Instance.activeObjectives.Sum(obj => obj.requiredCount);
        }
        public float GetTotalInitialObjectives()
        {
            return ObjectiveManager.Instance.activeObjectives.Sum(obj => obj.isProcedurallyGenerated ? obj.generatedCount.y : obj.initialRequiredCount);
        }
    }

    [System.Serializable]
    public class Objective
    {
        [Tooltip("The type of this objective. Created via scriptable object, it defines the event that will be listened to for this objective and the parameter type of that event.")]
        public ObjectiveType objectiveType;
        [Tooltip("Indicates whether this objective is procedurally generated.")]
        public bool isProcedurallyGenerated;
        [Tooltip("If true, required count will start at 0 the objective will automatically increase the required count based on the creation event.")]
        public bool autoCountRequiredCount;
        [Tooltip("The scriptable object parameter associated with this objective's event. Every time the event specified in objective type is sent with this specific scriptable object, the required objective count will decreased.")]
        [HideIf("IsProcedurallyGenerated")]
        public VisualizableScriptableObject scriptableObjectParameter;
        [HideIf("IsProcedurallyGenerated")]
        public int requiredCount;
        [ShowIf("IsProcedurallyGenerated"), MinMaxSlider(1, 20)]
        public Vector2Int generatedCount;
        public bool isCompleted;

        internal int initialRequiredCount;

        bool IsProcedurallyGenerated()
        {
            return isProcedurallyGenerated;
        }

    }


}