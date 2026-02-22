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
        public Image actionBarIcon;
        public TMP_Text actionText;
        public TMP_Text levelText;
        public TMP_Text costText;
        public Button onClickButton;
        public CanvasGroup canvasGroup;

        private void Start()
        {
            
        }

        private void Update()
        {
            DrawUI();
        }

        public void DrawUI()
        {
            if (actionBarItem != null)
            {
                if(actionBarIcon) 
                    actionBarIcon.sprite = actionBarItem.actionBarIcon;
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