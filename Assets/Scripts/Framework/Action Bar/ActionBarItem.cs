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
    public abstract class ActionBarItem : MonoBehaviour
    {
        public string actionName;
        [Header("References")]
        public Sprite actionBarIcon;
        public Button onClickButton;
        [Header("Cost Settings")]
        public CurrencyModel costCurrency;
        public float baseCost = 100;
        public float costIncrement = 50;
        public float costAcceleration = 10;
        [Header("Technical Settings")]
        public float visibilityCheckInterval = 0.1f;
        public float clickabilityCheckInterval = 0.1f;

        internal bool isVisible, isClickable;

        public int CurrentLevel
        {
            get => PlayerPrefs.GetInt(actionName + "_level", 1);
            set => PlayerPrefs.SetInt(actionName + "_level", value);
        }

        /// <summary>
        /// Multiplier for the cost curve. This can be used to adjust the cost curve to fit the game's economy or other elements like bonuses and such.
        /// </summary>
        public abstract float CostMultiplier { get; }

        // Start is called before the first frame update
        void Start()
        {
            InvokeRepeating("SetVisibility", 0, visibilityCheckInterval);
            InvokeRepeating("SetClickability", 0, clickabilityCheckInterval);

            onClickButton.onClick.AddListener(() => {
                EventManager.TriggerEvent(GameEvent.ACTION_CLICKED, new EventParam(
                    paramStr: actionName
                ));
                OnClick();
            });
        }

        // Update is called once per frame
        void Update()
        {

        }

        public int GetCost()
        {
            float costIncrement = this.costIncrement + (CurrentLevel - 2) * costAcceleration;
            return Mathf.RoundToInt(baseCost + (CurrentLevel - 1) * costIncrement);
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
}