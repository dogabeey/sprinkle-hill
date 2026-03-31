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

        internal bool isVisible, isClickable;

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

        private void SetVisibility()
        {
            isVisible = IsVisible();
        }
        private void SetClickability()
        {
            isClickable = IsClickable();
        }

        abstract public void OnClick();
        abstract public bool IsVisible();
        abstract public bool IsClickable();
    }
    public class IncrementalBonus : ActionBarItem
    {
        public override float BaseCost => 100;
        public override float CostIncrement => 0;
        public override float CostAcceleration => 0;

        public override bool IsClickable()
        {
            return CurrencyManager.Instance.GetCurrencyAmount(costCurrency) >= GetCost();
        }

        public override bool IsVisible()
        {
            return true;
        }

        public override void OnClick()
        {
            CurrencyManager.Instance.AddCurrency(costCurrency.currencyID, -GetCost());
            CurrentLevel++;
        }
    }
}
