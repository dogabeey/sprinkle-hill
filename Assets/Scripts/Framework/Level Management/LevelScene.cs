using DG.Tweening;
using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;


namespace Game
{
    [System.Serializable]
    public class CurrencyReward
    {
        public CurrencyModel type;
        public int amount;
    }

    public class LevelScene : MonoBehaviour
    {
        [HideInInspector] public bool isWin;
        [HideInInspector] public bool isLose;
        [HideInInspector] public bool isEnded;
        [HideInInspector] public bool isPaused;


        public static LevelScene Instance;

        public string levelName;
        [TextArea]
        public string winText;
        [TextArea]
        public string loseText;
        public List<CurrencyReward> rewards;

        protected virtual void Awake()
        {
            Instance = this;
            EventManager.TriggerEvent(GameEvent.LEVEL_STARTED, new EventParam());
        }

        public void CompleteLevel()
        {
            isWin = true;
        }
        public void FailLevel()
        {
            isLose = true;
        }


        virtual protected void Update()
        {
            if (isEnded) return;
            if (isWin) // PUT YOUR WIN CONDITIONS HERE
            {
                isEnded = true;
                EventParam param = new EventParam();
                EventManager.TriggerEvent(GameEvent.LEVEL_COMPLETED, param); // You can trigger this event anywhere and It will trigger On Win actions in the inspector, along with regular Level Completion events. This one also passes the time it took to win the level.
            }
            if (isLose) // PUT YOUR LOSE CONDITIONS HERE
            {
                isEnded = true;
                EventParam param = new EventParam();
                EventManager.TriggerEvent(GameEvent.LEVEL_FAILED, param); // You can trigger this event anywhere and It will trigger It will trigger On Lose actions in the inspector, along with regular Level Failure events. This one also passes the time it took to lose the level.
            }


        }

    }
}