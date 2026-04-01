using DG.Tweening;
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
    public class ActionBarView : UIElement
    {
        public ActionBarItem actionBarItem;
        public Image actionIcon;
        public GameObject lockedPanel;
        public TMP_Text actionText;
        public TMP_Text costText;
        public GameObject countTextPanel;
        public TMP_Text countText;
        public Button useButton;
        public Button buyButton; // not implemented yet
        public CanvasGroup canvasGroup;
        public LayoutElement layoutElement;

        public void Init(ActionBarItem actionBarItem)
        {
            this.actionBarItem = actionBarItem;
            Quaternion originalQuaternion = lockedPanel.transform.rotation;
            useButton.onClick.AddListener(() => {
                EventManager.TriggerEvent(GameEvent.ACTION_BAR_ITEM_CLICKED, new EventParam(
                    paramStr: actionBarItem.ActionName
                ));

                if(actionBarItem.IsAvailable())
                {
                    SoundManager.Instance.Play(ConstantManager.SOUNDS.EFFECTS.BUTTON_CLICK_SUCCESS);
                    actionBarItem.OnClick();
                }
                else
                {
                    SoundManager.Instance.Play(ConstantManager.SOUNDS.EFFECTS.BUTTON_CLICK_FAIL);
                    // Shake lockedpanel
                    if (lockedPanel)
                    {
                        lockedPanel.transform.DOShakeRotation(0.5f, new Vector3(0, 0, 10), 10, 90).SetEase(Ease.OutQuad).OnComplete(() => {
                            lockedPanel.transform.rotation = originalQuaternion;
                        });
                    }
                }
            });
            DrawUI();
        }

        public override void InitUI()
        {
            
        }

        public override void DrawUI()
        {
            if (actionBarItem != null)
            {
                if (actionIcon)
                    actionIcon.sprite = actionBarItem.actionBarIcon;
                if (lockedPanel)
                    lockedPanel.SetActive(!actionBarItem.IsAvailable());
                if (actionText) 
                    actionText.text = actionBarItem.ActionName;
                if (costText)
                    costText.text = actionBarItem.GetCost().ConvertToKMB();
                if (countTextPanel)
                    countTextPanel.SetActive(actionBarItem.CurrentCount > 0 && actionBarItem.IsAvailable());
                if (countText)
                    countText.text = actionBarItem.CurrentCount.ToString();
                if (useButton)
                    useButton.interactable = actionBarItem.IsClickable();
                if (canvasGroup)
                    canvasGroup.alpha = actionBarItem.IsVisible() ? 1 : 0;
                if (layoutElement)
                    layoutElement.ignoreLayout = !actionBarItem.IsVisible();
            }
        }
    }
}