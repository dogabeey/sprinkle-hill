using DG.Tweening;
using MobileHapticsProFreeEdition;
using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;


namespace Game
{

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
        [FoldoutGroup("Extra Moves")]
        public CurrencyReward extraMoveCost;
        [FoldoutGroup("Extra Moves")]
        public int extraMovesGiven;

        protected virtual void Awake()
        {
            Instance = this;
            EventManager.TriggerEvent(GameEvent.LEVEL_STARTED, new EventParam());
        }

        public void CompleteLevel()
        {
            isWin = true;
        }
        public void FailLevel(string failReason = "")
        {
            loseText = string.IsNullOrEmpty(failReason) ? loseText : failReason;
            isLose = true;
        }


        virtual protected void LateUpdate()
        {
            if (isEnded) return;
            if (isWin) // PUT YOUR WIN CONDITIONS HERE
            {
                isEnded = true;
                GridHelper.TriggerHaptic(HapticModes.Confirm);
                EventParam param = new EventParam();
                EventManager.TriggerEvent(GameEvent.LEVEL_COMPLETED, param); // You can trigger this event anywhere and It will trigger On Win actions in the inspector, along with regular Level Completion events. This one also passes the time it took to win the level.
            }
            if (isLose) // PUT YOUR LOSE CONDITIONS HERE
            {
                isEnded = true;
                GridHelper.TriggerHaptic(HapticModes.Failure);
                EventParam param = new EventParam();
                EventManager.TriggerEvent(GameEvent.LEVEL_FAILED, param); // You can trigger this event anywhere and It will trigger It will trigger On Lose actions in the inspector, along with regular Level Failure events. This one also passes the time it took to lose the level.
            }


        }

        internal void RestoreStateBeforeLoseCondition()
        {
            isLose = false;
            isPaused = false;
            isEnded = false;
        }
    }
    [System.Serializable]
    public class CurrencyReward
    {
        public CurrencyModel type;
        public int amount;
    }

}