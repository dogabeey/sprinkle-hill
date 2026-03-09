using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Game
{
    public enum GameEvents
    {
        COLLECTIBLE_EARNED,
        OBJECTIVE_COMPLETED,
        OBJECTIVE_FAILED,
        LEVEL_COMPLETED,
        LEVEL_FAILED,
        LEVEL_STARTED,
        CURRENT_WORLD_CHANGED,
        ELEMENT_SELECTED,
        ELEMENT_MATCHED,
        ELEMENT_DESTROYED
    }

    [CreateAssetMenu(fileName = "EventManager", menuName = "Game/Managers/EventManager")]
    public class EventManager : ScriptableObject, IManager
    {

        private Dictionary<string, Action<EventParam>> eventDictionary = new Dictionary<string, Action<EventParam>>();

        public static EventManager instance => GameManager.Instance.eventManager;

        public void OnInit()
        {
        }

        public void OnUpdate()
        {
        }

        public void OnApplicationPause()
        {
        }
        public void OnApplicationQuit()
        {
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
        }

        public static void StartListening(GameEvents eventName, Action<EventParam> listener)
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
        }

        public static void StopListening(GameEvents eventName, Action<EventParam> listener)
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

        public static void TriggerEvent(GameEvents eventName)
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

        public static void TriggerEvent(GameEvents eventName, EventParam eventParam)
        {
            TriggerEvent(eventName.ToString(), eventParam);
        }

    }


    public class EventParam
    {
        public GameObject paramObj;
        public int paramInt;
        public float paramFloat;
        public string paramStr;
        public Type paramType;
        public bool paramBool;
        public Dictionary<string, object> paramDictionary;

        public EventParam()
        {

        }

        public EventParam(Dictionary<string, object> paramDictionary)
        {
            this.paramDictionary = paramDictionary;
        }

        public EventParam(GameObject paramObj = null, int paramInt = 0, float paramFloat = 0f, string paramStr = "", Type paramType = null, Dictionary<string, object> paramDictionary = null,
        Vector3[] vectorList = null, bool paramBool = false)
        {
            this.paramObj = paramObj;
            this.paramInt = paramInt;
            this.paramFloat = paramFloat;
            this.paramStr = paramStr;
            this.paramType = paramType;
            this.paramDictionary = paramDictionary;
            this.paramBool = paramBool;
        }
    }
}