using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Game;

public enum GameEvent
{
    COLLECTIBLE_EARNED,

    OBJECTIVES_INITIALIZED,
    OBJECTIVE_COMPLETED,
    OBJECTIVE_FAILED,

    LEVEL_COMPLETED,
    LEVEL_FAILED,
    LEVEL_STARTED,

    CURRENT_WORLD_CHANGED,


    // Element and Grid Events
    ELEMENT_SELECTED,
    ELEMENT_MATCHED,
    ELEMENT_DESTROYED,
    ELEMENTS_SWAPPED,
    SWAP_FAILED,
    MATCH_DETECTED,
    COMBO_TRIGGERED,
    GRAVITY_STARTED,
    GRAVITY_COMPLETED,
    ELEMENTS_REFILLED,
    GRID_INITIALIZED,
    GRID_STABLE,
    
    // Currency Events
    CURRENCY_EARNED,
    CURRENCY_SPENT,
    
    // Action Bar Events
    ACTION_USED,
    ACTION_CLICKED,
    
    // UI Events
    SCREEN_OPENED,
    SCREEN_CLOSED,
    
    // Special Elements
    BOX_DESTROYED,
    SPECIAL_ELEMENT_ACTIVATED,
    
    // Ad Events
    AD_SHOWN,
    AD_CLOSED,
    AD_FAILED,
    REWARDED_AD_SHOWN,
    REWARDED_AD_COMPLETED,
    REWARDED_AD_FAILED,
    
    // Sound Events
    SOUND_PLAYED,
    MUSIC_STARTED,
    MUSIC_STOPPED,
    SOUND_SETTINGS_CHANGED
}

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
public class EventManager : ScriptableObject, IManager
{

    private Dictionary<string, Action<EventParam>> eventDictionary = new Dictionary<string, Action<EventParam>>();

    [Header("Inspector Debug Info")]
    [SerializeField, Tooltip("Shows all currently active event listeners (Runtime Only)")]
    private List<EventListenerInfo> activeListeners = new List<EventListenerInfo>();
    
    [SerializeField, Tooltip("Total number of active listeners")]
    private int totalListenerCount = 0;

    public static EventManager instance => GameManager.Instance.eventManager;

    public void OnInit()
    {
        UpdateInspectorInfo();
    }

    public void OnUpdate()
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
        if (instance.eventDictionary.TryGetValue(eventName, out thisEvent))
        {
            //Add more event to the existing one
            thisEvent += listener;

            //Update the Dictionary
            instance.eventDictionary[eventName] = thisEvent;
        }
        else
        {
            //Add event to the Dictionary for the first time
            thisEvent += listener;
            instance.eventDictionary.Add(eventName, thisEvent);
        }
        
#if UNITY_EDITOR
        instance.UpdateInspectorInfo();
#endif
    }

    public static void StartListening(GameEvent eventName, Action<EventParam> listener)
    {
        StartListening(eventName.ToString(), listener);
    }

    public static void StopListening(string eventName, Action<EventParam> listener)
    {
        if (GameManager.Instance.eventManager == null) return;
        Action<EventParam> thisEvent;
        if (instance.eventDictionary.TryGetValue(eventName, out thisEvent))
        {
            //Remove event from the existing one
            thisEvent -= listener;

            //Update the Dictionary
            instance.eventDictionary[eventName] = thisEvent;
        }
        
#if UNITY_EDITOR
        instance.UpdateInspectorInfo();
#endif
    }

    public static void StopListening(GameEvent eventName, Action<EventParam> listener)
    {
        StopListening(eventName.ToString(), listener);
    }

    public static void TriggerEvent(string eventName)
    {
        EventParam eventParam = new EventParam();
        Action<EventParam> thisEvent = null;
        if (instance.eventDictionary.TryGetValue(eventName, out thisEvent))
        {
            thisEvent.Invoke(eventParam);
            // OR USE  instance.eventDictionary[eventName](eventParam);
        }
    }

    public static void TriggerEvent(GameEvent eventName)
    {
        TriggerEvent(eventName.ToString());
    }

    public static void TriggerEvent(string eventName, EventParam eventParam)
    {
        Action<EventParam> thisEvent = null;
        if (instance.eventDictionary.TryGetValue(eventName, out thisEvent))
        {
            thisEvent.Invoke(eventParam);
            // OR USE  instance.eventDictionary[eventName](eventParam);
        }
    }

    public static void TriggerEvent(GameEvent eventName, EventParam eventParam)
    {
        TriggerEvent(eventName.ToString(), eventParam);
    }

}


public class EventParam
{
    public GameObject paramObj;
    public ScriptableObject paramScriptable;
    public int paramInt;
    public float paramFloat;
    public string paramStr;
    public Type paramType;
    public Vector3[] vectorList;
    public bool paramBool;
    public Dictionary<string, object> paramDictionary;

    public EventParam()
    {

    }

    public EventParam(Dictionary<string, object> paramDictionary)
    {
        this.paramDictionary = paramDictionary;
    }

    public EventParam(GameObject paramObj = null, ScriptableObject paramScriptable = null, int paramInt = 0, float paramFloat = 0f, string paramStr = "", Type paramType = null, Dictionary<string, object> paramDictionary = null,
    Vector3[] vectorList = null, bool paramBool = false)
    {
        this.paramObj = paramObj;
        this.paramScriptable = paramScriptable;
        this.paramInt = paramInt;
        this.paramFloat = paramFloat;
        this.paramStr = paramStr;
        this.paramType = paramType;
        this.paramDictionary = paramDictionary;
        this.vectorList = vectorList;
        this.paramBool = paramBool;
    }
}