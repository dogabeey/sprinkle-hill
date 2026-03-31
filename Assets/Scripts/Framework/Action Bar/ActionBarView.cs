using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game
{
    /// <summary>
    /// Action Bar View draws the action bar item's properties into UI elements.
    /// </summary>
    public class ActionBarView : MonoBehaviour
    {
        public ActionBarItem actionBarItem;
        public Image actionIcon;
        public TMP_Text actionText;
        public TMP_Text levelText;
        public TMP_Text costText;
        public TMP_Text countText;
        public Button onClickButton;
        public CanvasGroup canvasGroup;

        public void Init(ActionBarItem actionBarItem)
        {
            this.actionBarItem = actionBarItem;
            onClickButton.onClick.AddListener(() => {
                EventManager.TriggerEvent(GameEvent.ACTION_CLICKED, new EventParam(
                    paramStr: actionBarItem.actionName
                ));
                actionBarItem.OnClick();
            });
            DrawUI();
        }
        private void Update()
        {
            DrawUI();
        }

        public void DrawUI()
        {
            if (actionBarItem != null)
            {
                if(actionIcon) 
                    actionIcon.sprite = actionBarItem.actionBarIcon;
                if(actionText) 
                    actionText.text = actionBarItem.actionName;
                if(levelText) 
                    levelText.text = "LEVEL " + actionBarItem.CurrentLevel.ToString();
                if (costText)
                    costText.text = actionBarItem.GetCost().ConvertToKMB();
                if (onClickButton)
                    onClickButton.interactable = actionBarItem.isClickable;
                if (canvasGroup)
                    canvasGroup.alpha = actionBarItem.isVisible ? 1 : 0;
            }
        }
    }
}