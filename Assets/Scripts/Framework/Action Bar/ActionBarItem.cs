using DG.Tweening;
using Game;
using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Game
{
    /// <summary>
    /// This is an action bar item which the player can click to perform an action. It can cost any currency. New action bar items can be created by extending this class. 
    /// You can configure conditions which decides when the button will be visible or clickable. 
    /// </summary>
    [Serializable]
    public abstract class ActionBarItem
    {
        private const float visibilityCheckInterval = 0.1f;
        private const float clickabilityCheckInterval = 0.1f;

        public abstract string ActionName { get; }
        public Sprite actionBarIcon;
        public CurrencyModel costCurrency;
        [ReadOnly] public int startingCount = 10; // This is only used for the first time the player gets this action. After that, the count will be saved in PlayerPrefs and this value will not be used anymore. This is useful for testing and debugging.

        internal bool isVisible, isClickable, isAvailable;

        public abstract float BaseCost { get; }
        public abstract float CostIncrement { get; }
        public abstract float CostAcceleration { get; }
        public abstract string VisibilityExplanation { get; } // Explanation for why the action is not visible when it is not visible.
        public abstract string ClickabilityExplanation { get; } // Explanation for why the action is not clickable when it is not clickable.
        public abstract string AvailabilityExplanation { get; } // Explanation for why the action is not available when it is not available.

        

        public int CurrentCount // TODO: change this to Isaveable when implementing save system
        {
            get => PlayerPrefs.GetInt(ActionName + "_count", startingCount);
            set => PlayerPrefs.SetInt(ActionName + "_count", value);
        }

        abstract public int GetCost();

        abstract public void OnClick();
        /// <summary>
        /// Determines whether the object is visible in the list at all. This is used to hide or show the object in the list. If the object is not visible, it will not take up any space in the list.
        /// </summary>
        /// <returns>true if the object is visible; otherwise, false.</returns>
        abstract public bool IsVisible();
        /// <summary>
        /// Determines whether the object is clickable. This is used to enable or disable the button but the object will still take up space in the list. This is useful when you want to show the player that there is an action they can perform but they don't have enough resources to perform it yet.
        /// </summary>
        /// <returns>true if the object is clickable; otherwise, false.</returns>
        abstract public bool IsClickable();
        /// <summary>
        /// Determines whether the object is available. This is used to show or hide the locked panel.
        /// </summary>
        /// <returns>true if the object is available; otherwise, false.</returns>
        abstract public bool IsAvailable();
    }
    [Serializable]
    public abstract class BonusPremiumAction : ActionBarItem
    {
        public int unlockedLevel;
    }

    [Serializable]
    public class AddTimeAction : BonusPremiumAction
    {
        public int addedTime = 30;
        public ParticleSystem actionSuccessParticle;

        public override string ActionName => "Add Time";
        public override float BaseCost => 0;
        public override float CostIncrement => 0;
        public override float CostAcceleration => 0;
        public override string VisibilityExplanation => "";
        public override string ClickabilityExplanation => "";
        public override string AvailabilityExplanation => $"Reach Level {unlockedLevel}";

        public override bool IsClickable()
        {
            return CurrentCount > 0;
        }

        public override int GetCost()
        {
            return 0;
        }

        public override bool IsVisible()
        {
            return true;
        }

        public override bool IsAvailable()
        {
            return World.Instance.lastPlayedLevelIndex >= unlockedLevel;
        }

        public override void OnClick()
        {
            CurrentCount--;
            (GameManager.Instance.CurrentLevel as LevelScene_Match3Game).timer += addedTime;

            Transform addTimeActionTransform = GameManager.Instance.actionBarManager.GetActionBarView(this).transform;
            Transform timerTextTransform = GameManager.Instance.upperPanelUI.timerText.transform;

            // Send a particle from the action button to the timer text
            if (actionSuccessParticle != null)
            {
                ParticleSystem particle = GameObject.Instantiate(actionSuccessParticle, addTimeActionTransform.position, Quaternion.identity);
                particle.transform.DOMove(timerTextTransform.position, 1f).OnComplete(() => GameObject.Destroy(particle.gameObject));
            }

        }
    }
}
