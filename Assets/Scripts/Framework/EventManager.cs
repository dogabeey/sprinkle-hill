using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Game;

[System.Serializable]
public class EventListenerInfo
{
    public string eventName;
    public int listenerCount;
    public List<string> listenerTargets = new List<string>();

    public EventListenerInfo(string eventName, int listenerCount, List<string> listenerTargets)
    {
        this.eventName = eventName;
        this.listenerCount = listenerCount;
        this.listenerTargets = listenerTargets;
    }
}

[CreateAssetMenu(fileName = "EventManager", menuName = "Game/Managers/EventManager")]
public class EventManager : MonoBehaviour
{
    public static EventManager Instance => GameManager.Instance ? GameManager.Instance.eventManager : null;

    private Dictionary<string, Action<EventParam>> eventDictionary = new Dictionary<string, Action<EventParam>>();

    [Header("Settings")]
    public float eventQueueProcessingInterval = 0.01f; // Time in seconds between processing the event queue
    [Header("Inspector Debug Info")]
    [SerializeField, Tooltip("Shows all currently active event listeners (Runtime Only)")]
    private List<EventListenerInfo> activeListeners = new List<EventListenerInfo>();
    [SerializeField, Tooltip("Total number of active listeners")]
    private int totalListenerCount = 0;
    [SerializeField]
    private Queue<KeyValuePair<string, EventParam>> eventDictionaryQueue = new Queue<KeyValuePair<string, EventParam>>();

    public void Start()
    {
#if UNITY_EDITOR
        UpdateInspectorInfo();
#endif
        StartCoroutine(TriggerEventQueueCoroutine());
    }

    private IEnumerator TriggerEventQueueCoroutine()
    {
        while (enabled)
        {
            if(eventDictionaryQueue.Count > 0)
            {
                KeyValuePair<string, EventParam> kvp = eventDictionaryQueue.Dequeue();
                TriggerEvent(kvp.Key, kvp.Value);
            }
            yield return new WaitForSeconds(eventQueueProcessingInterval);
        }
    }

    public void Update()
    {
#if UNITY_EDITOR
        UpdateInspectorInfo();
#endif
    }

    public void OnApplicationPause()
    {
    }
    
    public void OnApplicationQuit()
    {
        ClearAllListeners();
    }

    private void OnDisable()
    {
#if UNITY_EDITOR
        // Clear listeners when exiting play mode
        if (!UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode && UnityEditor.EditorApplication.isPlaying)
        {
            ClearAllListeners();
        }
#endif
    }

    private void UpdateInspectorInfo()
    {
        activeListeners.Clear();
        totalListenerCount = 0;

        foreach (var kvp in eventDictionary)
        {
            if (kvp.Value != null)
            {
                Delegate[] invocationList = kvp.Value.GetInvocationList();
                int count = invocationList.Length;
                totalListenerCount += count;

                List<string> targets = new List<string>();
                foreach (Delegate del in invocationList)
                {
                    if (del.Target != null)
                    {
                        targets.Add($"{del.Target.GetType().Name}.{del.Method.Name}");
                    }
                    else
                    {
                        targets.Add($"Static.{del.Method.Name}");
                    }
                }

                activeListeners.Add(new EventListenerInfo(kvp.Key, count, targets));
            }
        }
    }

    private void ClearAllListeners()
    {
        eventDictionary.Clear();
        activeListeners.Clear();
        totalListenerCount = 0;
    }

    public static void StartListening(string eventName, Action<EventParam> listener)
    {
        Action<EventParam> thisEvent;
        if (Instance.eventDictionary.TryGetValue(eventName, out thisEvent))
        {
            //Add more event to the existing one
            thisEvent += listener;

            //Update the Dictionary
            Instance.eventDictionary[eventName] = thisEvent;
        }
        else
        {
            //Add event to the Dictionary for the first time
            thisEvent += listener;
            Instance.eventDictionary.Add(eventName, thisEvent);
        }
        
#if UNITY_EDITOR
        Instance.UpdateInspectorInfo();
#endif
    }

    public static void StartListening(GameEvent eventName, Action<EventParam> listener)
    {
        StartListening(eventName.ToString(), listener);
    }

    public static void StopListening(string eventName, Action<EventParam> listener)
    {
        if (GameManager.Instance && GameManager.Instance.eventManager == null) return;
        if(Instance == null) return;
        if(Instance.eventDictionary == null) return;
        Action<EventParam> thisEvent;
        if (Instance.eventDictionary.TryGetValue(eventName, out thisEvent))
        {
            //Remove event from the existing one
            thisEvent -= listener;

            //Update the Dictionary
            Instance.eventDictionary[eventName] = thisEvent;
        }
        
#if UNITY_EDITOR
        Instance.UpdateInspectorInfo();
#endif
    }

    public static void StopListening(GameEvent eventName, Action<EventParam> listener)
    {
        StopListening(eventName.ToString(), listener);
    }

    private static void TriggerEvent(string eventName)
    {
        EventParam eventParam = new EventParam();
        Action<EventParam> thisEvent = null;
        if (Instance.eventDictionary.TryGetValue(eventName, out thisEvent))
        {
            if(thisEvent == null)
            {
                Debug.LogWarning($"Event '{eventName}' has some null listener(s). This might result events to notifiy its listeners incorrectly");
            }
            else
            {
                thisEvent.Invoke(eventParam);
            }
        }
    }

    public static void TriggerEvent(GameEvent eventName)
    {
        Instance.eventDictionaryQueue.Enqueue(new KeyValuePair<string, EventParam>(eventName.ToString(), new EventParam()));
    }

    private static void TriggerEvent(string eventName, EventParam eventParam)
    {
        if (eventParam == null)
            eventParam = new EventParam();

        eventParam.PrepareForDispatch();

        Action<EventParam> thisEvent = null;
        if (Instance.eventDictionary.TryGetValue(eventName, out thisEvent))
        {
            if (thisEvent == null)
            {
                Debug.LogWarning($"Event '{eventName}' has some null listener(s). This might result events to notifiy its listeners incorrectly");
            } 
            else
            {
                thisEvent.Invoke(eventParam);
                AnalyticsManager.SendEvent(eventName, eventParam.Payload);
            }
        }
    }

    public static void TriggerEvent(GameEvent eventName, EventParam eventParam)
    {
        Instance.eventDictionaryQueue.Enqueue(new KeyValuePair<string, EventParam>(eventName.ToString(), eventParam));
    }

}
