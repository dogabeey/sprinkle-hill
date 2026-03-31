using Game;
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
    public abstract class ActionBarItem
    {
        private const float visibilityCheckInterval = 0.1f;
        private const float clickabilityCheckInterval = 0.1f;

        public string actionName;
        [Header("References")]
        public Sprite actionBarIcon;
        public Button onClickButton;
        [Header("Cost Settings")]
        public CurrencyModel costCurrency;
        public abstract float BaseCost { get; }
        public abstract float CostIncrement { get; }
        public abstract float CostAcceleration { get; }

        internal bool isVisible, isClickable, isAvailable;

        public int CurrentLevel // TODO: change this to Isaveable when implementing save system
        {
            get => PlayerPrefs.GetInt(actionName + "_level", 1);
            set => PlayerPrefs.SetInt(actionName + "_level", value);
        }
        public int CurrentCount // TODO: change this to Isaveable when implementing save system
        {
            get => PlayerPrefs.GetInt(actionName + "_count", 0);
            set => PlayerPrefs.SetInt(actionName + "_count", value);
        }

        public virtual int GetCost()
        {
            float costIncrement = this.CostIncrement + (CurrentLevel - 2) * CostAcceleration;
            return Mathf.RoundToInt(BaseCost + (CurrentLevel - 1) * costIncrement);
        }

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
    public class BonusPremiumAction : ActionBarItem
    {
        public override float BaseCost => 100;
        public override float CostIncrement => 0;
        public override float CostAcceleration => 0;

        public int unlockedLevel = 1;

        public override bool IsClickable()
        {
            return CurrencyManager.Instance.GetCurrencyAmount(costCurrency) >= GetCost() && IsAvailable();
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
            CurrencyManager.Instance.AddCurrency(costCurrency.currencyID, -GetCost());
            CurrentLevel++;
        }
    }
}
