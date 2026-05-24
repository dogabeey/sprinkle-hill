using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine; using Game.EventManagement;
using UnityEngine.UI;
using Game.EventManagement;

namespace Game
{
    /// <summary>
    /// Action Bar View draws the action bar item's properties into UI elements.
    /// </summary>
    public class ActionBarView : UIElement
    {
        public ActionBarItem actionBarItem;
        public Image actionIcon;
        public Image lockedIcon;
        public Image selectionIcon;
        public GameObject lockedPanel;
        public TMP_Text unlockConditionText;
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
            useButton.onClick.AddListener(() =>
            {
                OnUseButtonClicked(actionBarItem);
            });
            buyButton.onClick.AddListener(() =>
            {
                OnBuyButtonClicked(actionBarItem);
            });
            
            EventManager.StartListening(GameEvent.ACTION_SUCCESSFUL, OnActionSuccessful);
            
            DrawUI();
        }

        private void OnActionSuccessful(EventParam param)
        {
            RefreshAllActionBarViews();
        }

        private void OnDestroy()
        {
            EventManager.StopListening(GameEvent.ACTION_SUCCESSFUL, OnActionSuccessful);
        }

        private void OnUseButtonClicked(ActionBarItem actionBarItem)
        {
            if (actionBarItem.IsAvailable())
            {
                SoundManager.Instance.Play(ConstantManager.SOUNDS.EFFECTS.BUTTON_CLICK_SUCCESS);
                actionBarItem.OnClick();
                RefreshAllActionBarViews();
            }
            else
            {
                SoundManager.Instance.Play(ConstantManager.SOUNDS.EFFECTS.BUTTON_CLICK_FAIL);
                // Shake lockedpanel
                if (lockedPanel)
                {
                    ShakeLockedPanel();
                }
            }

            EventManager.TriggerEvent(GameEvent.ACTION_BAR_ITEM_CLICKED, new EventParam(
                paramStr: actionBarItem.ItemName
            ));
        }

        private void RefreshAllActionBarViews()
        {
            if (GameManager.Instance != null && ActionBarManager.Instance != null)
            {
                foreach (ActionBarView view in ActionBarManager.Instance.actionBarViews)
                {
                    if (view != null)
                        view.DrawUI();
                }
            }
        }
        private void OnBuyButtonClicked(ActionBarItem actionBarItem)
        {
            ScreenManager.Instance.Show(Screens.BuyMenu, new EventParam(
                paramValue: actionBarItem
            ));
        }

        private void ShakeLockedPanel()
        {
            Tween lockedPanelShake = null;
            Quaternion originalQuaternion = lockedPanel.transform.rotation;
            if (lockedPanelShake != null && lockedPanelShake.IsPlaying())
            {
                lockedPanel.transform.rotation = originalQuaternion;
                lockedPanelShake.Restart();
            }
            else
            {
                lockedPanelShake = lockedPanel.transform.DOShakeRotation(0.5f, new Vector3(0, 0, 10), 10, 90).SetEase(Ease.OutQuad);
            }
        }

        public override void InitUI()
        {
            
        }

        public override void DrawUI()
        {
            if (actionBarItem != null)
            {
                if (actionIcon)
                {
                    actionIcon.enabled = actionBarItem.IsAvailable();
                    actionIcon.sprite = actionBarItem.ActionBarIcon;
                }
                if (lockedIcon)
                    lockedIcon.enabled = actionIcon && !actionIcon.enabled;
                if (selectionIcon)
                    selectionIcon.enabled = actionBarItem.IsSelected();
                if (actionText) 
                    actionText.text = actionBarItem.ItemName;
                if(buyButton)
                    buyButton.gameObject.SetActive(actionBarItem.CostDefinesBuyability && actionBarItem.currentCount <= 0 && actionBarItem.IsAvailable());
                if (costText)
                    costText.text = actionBarItem.GetCost().ConvertToKMB();
                if (unlockConditionText)
                    unlockConditionText.text = actionBarItem.AvailabilityExplanation;
                if (countTextPanel)
                    countTextPanel.SetActive(actionBarItem.IsAvailable());
                if (countText)
                    countText.text = actionBarItem.currentCount > 0 ? actionBarItem.currentCount.ToString() : "+";
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