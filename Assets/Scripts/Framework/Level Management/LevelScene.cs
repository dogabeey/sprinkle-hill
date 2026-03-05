using DG.Tweening;
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

        public static LevelScene Instance;

        public string levelName;

        virtual protected void Awake()
        {
            Instance = this;
            EventManager.TriggerEvent(ConstantManager.GameEvents.LEVEL_STARTED, new EventParam());
        }

        virtual protected void Update()
        {
            if (isEnded) return;
            if (isWin) // PUT YOUR WIN CONDITIONS HERE
            {
                isEnded = true;
                EventParam param = new EventParam();
                EventManager.TriggerEvent(ConstantManager.GameEvents.LEVEL_COMPLETED, param); // You can trigger this event anywhere and It will trigger On Win actions in the inspector, along with regular Level Completion events. This one also passes the time it took to win the level.
            }
            if (isLose) // PUT YOUR LOSE CONDITIONS HERE
            {
                isEnded = true;
                EventParam param = new EventParam();
                EventManager.TriggerEvent(ConstantManager.GameEvents.LEVEL_FAILED, param); // You can trigger this event anywhere and It will trigger It will trigger On Lose actions in the inspector, along with regular Level Failure events. This one also passes the time it took to lose the level.
            }


        }

    }
}