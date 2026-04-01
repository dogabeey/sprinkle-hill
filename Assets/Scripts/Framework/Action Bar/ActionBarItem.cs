using Game;
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
        [Header("References")]
        public Sprite actionBarIcon;
        [Header("Cost Settings")]
        public CurrencyModel costCurrency;
        public abstract float BaseCost { get; }
        public abstract float CostIncrement { get; }
        public abstract float CostAcceleration { get; }
        public abstract string VisibilityExplanation { get; } // Explanation for why the action is not visible when it is not visible.
        public abstract string ClickabilityExplanation { get; } // Explanation for why the action is not clickable when it is not clickable.
        public abstract string AvailabilityExplanation { get; } // Explanation for why the action is not available when it is not available.

        internal bool isVisible, isClickable, isAvailable;

        public int CurrentCount // TODO: change this to Isaveable when implementing save system
        {
            get => PlayerPrefs.GetInt(ActionName + "_count", 0);
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
        public abstract int UnlockedLevel { get;  }
    }

    [Serializable]
    public class AddTimeAction : BonusPremiumAction
    {
        public override string ActionName => "Add Time";
        public override float BaseCost => 0;
        public override float CostIncrement => 0;
        public override float CostAcceleration => 0;
        public override int UnlockedLevel => 5;
        public override string VisibilityExplanation => "";
        public override string ClickabilityExplanation => "";
        public override string AvailabilityExplanation => $"Reach level {UnlockedLevel}.";

        public override bool IsClickable()
        {
            return true;
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
            return World.Instance.lastPlayedLevelIndex >= UnlockedLevel;
        }

        public override void OnClick()
        {

        }
    }
}
