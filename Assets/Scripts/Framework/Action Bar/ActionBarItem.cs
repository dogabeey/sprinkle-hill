using DG.Tweening;
using Game;
using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Game
{
    public interface IBuyable
    {
        public int GetCost();
        void Buy(int count);

        public string ActionName { get; }
        public string ActionDescription { get; }
        public CurrencyModel CostCurrency { get; }
        public List<BuyBundle> BuyConfig { get; }

        [Serializable]
        public class BuyBundle
        {
            public int buyCount; // The count at which the buy sprite will change to the specified sprite. If the count is greater than or equal to the count threshold, the buy sprite will be used. This is useful for assigning different sprites when you buy in greater bundles.
            public float discountPercentage; // The discount percentage to be applied when the count threshold is reached. For example, if the discount percentage is 0.1 (10%), and the original cost is 100, the cost will be reduced to 90 when the count threshold is reached. This is useful for encouraging players to buy in greater bundles by offering them a discount.
            public Sprite BuySprite; // The sprite to be used when the count is greater than or equal to the count threshold.

            public BuyBundle(int buyCount, float discountPercentage, Sprite buySprite)
            {
                this.buyCount = buyCount;
                this.discountPercentage = discountPercentage;
                BuySprite = buySprite;
            }
        }
    }

    /// <summary>
    /// This is an action bar item which the player can click to perform an action. It can cost any currency. New action bar items can be created by extending this class. 
    /// You can configure conditions which decides when the button will be visible or clickable. 
    /// </summary>
    [Serializable]
    public abstract class ActionBarItem : IBuyable
    {
        public abstract string ActionName { get; }
        public abstract string ActionDescription { get; }
        public abstract Sprite ActionBarIcon { get; }
        public abstract CurrencyModel CostCurrency { get; }
        public abstract ParticleSystem ActionSuccessParticle { get; }
        public int startingCount; // This is only used for the first time the player gets this action. After that, the count will be saved in PlayerPrefs and this value will not be used anymore. This is useful for testing and debugging.

        internal bool isVisible, isClickable, isAvailable;

        public abstract float BaseCost { get; }
        public abstract float CostIncrement { get; }
        public abstract float CostAcceleration { get; }
        public abstract string VisibilityExplanation { get; } // Explanation for why the action is not visible when it is not visible.
        public abstract string ClickabilityExplanation { get; } // Explanation for why the action is not clickable when it is not clickable.
        public abstract string AvailabilityExplanation { get; } // Explanation for why the action is not available when it is not available.
        public abstract bool CostDefinesBuyability { get; } // If true, the cost of the action will be used to determine whether the action is clickable. If false, the clickability will be determined by other conditions in IsClickable() method. This is useful when you want to have actions that are only limited by count but not by resources, or actions that are limited by resources but not by count.


        public int CurrentCount // TODO: change this to Isaveable when implementing save system
        {
            get => PlayerPrefs.GetInt(ActionName + "_count", startingCount);
            set => PlayerPrefs.SetInt(ActionName + "_count", value);
        }

        public List<IBuyable.BuyBundle> buyConfig;

        public List<IBuyable.BuyBundle> BuyConfig => buyConfig;
        public int[] BuyChoices => new int[] { 1, 5, 25 };

        public void Buy(int count)
        {
            int cost = GetCost() * count;
            if (CostCurrency != null)
            {
                if (CurrencyManager.Instance.GetCurrencyAmount(CostCurrency) >= cost)
                {
                    CurrencyManager.Instance.AddCurrency(CostCurrency, -cost);
                    CurrentCount += count;
                }
                else
                {
                    Debug.LogWarning("Not enough currency to buy " + ActionName);
                }
            }
            else
            {
                Debug.LogWarning("CostCurrency is not defined for " + ActionName);
            }
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
        virtual public bool IsClickable()
        {
            if (GameManager.Instance != null && GameManager.Instance.tutorialManager != null && GameManager.Instance.tutorialManager.ShouldDisableActionBar)
                return false;

            return CurrentCount > 0 || !IsAvailable();
        }
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
        public int buyCost = 50;
        public override bool CostDefinesBuyability => true;

        private IEnumerable GetAllFeatures()
        {
            ValueDropdownList<UnlockableFeature> features = new ValueDropdownList<UnlockableFeature>();
            foreach (UnlockableFeature feature in GameManager.Instance.featureTracker.features)
            {
                features.Add(feature.featureName, feature);
            }
            return features;
        }
        public override bool IsAvailable()
        {
            return World.Instance.lastPlayedLevelIndex >= unlockedLevel /* || (tiedFeature != null && tiedFeature.IsUnlocked(World.Instance.lastPlayedLevelIndex))*/;
        }

        public override int GetCost()
        {
            return buyCost;
        }
    }

    [Serializable]
    public class AddTimeAction : BonusPremiumAction
    {
        public int addedTime = 30;

        public override string ActionName => "Add Time";
        public override string ActionDescription => $"Adds {addedTime} seconds to the timer.";
        public override float BaseCost => 0;
        public override float CostIncrement => 0;
        public override float CostAcceleration => 0;
        public override string VisibilityExplanation => "";
        public override string ClickabilityExplanation => "";
        public override string AvailabilityExplanation => $"Level {unlockedLevel}";

        public override Sprite ActionBarIcon => GameManager.Instance.gfxManager.addTimeIcon;

        public override CurrencyModel CostCurrency => GameManager.Instance.cashCurrency;

        public override ParticleSystem ActionSuccessParticle => GameManager.Instance.gfxManager.addTimePowerupTrailParticlePrefab;

        public override bool IsVisible()
        {
            // Return true if current level stage is a timer level, otherwise return false. This is to ensure that the add time action is only visible in timer levels.
            return GameManager.Instance != null &&
                   GameManager.Instance.CurrentLevel != null &&
                   GameManager.Instance.CurrentLevel is LevelScene_Match3Game levelScene &&
                   levelScene.levelLimitType == LevelEditor.LevelLimitType.Timer;
        }

        public override void OnClick()
        {


            // Send a particle from the action button to the timer text
            GameManager.Instance.StartCoroutine(AddTimeEffect());

        }

        private IEnumerator AddTimeEffect()
        {
            CurrentCount--;
            if (ActionSuccessParticle != null)
            {
                Transform addTimeActionTransform = GameManager.Instance.actionBarManager.GetActionBarView(this).transform;
                Transform timerTextTransform = GameManager.Instance.upperPanelUI.timerText.transform;
                ParticleSystem particle = GameObject.Instantiate(ActionSuccessParticle, addTimeActionTransform.position, Quaternion.identity);
                particle.transform.DOMove(timerTextTransform.position, 1f).OnComplete(() => {
                    GameManager.Instance.upperPanelUI.timerText.transform.DOScale(1.22f, 0.2f).SetEase(Ease.Linear).OnComplete(() =>
                    {
                        GameManager.Instance.upperPanelUI.timerText.transform.DOScale(1f, 0.2f).SetEase(Ease.Linear);
                    });
                    GameObject.Destroy(particle.gameObject); 
                });
                yield return new WaitForSeconds(1f);
            }
            (GameManager.Instance.CurrentLevel as LevelScene_Match3Game).timer += addedTime;
        }

    }
    [Serializable]
    public class AddMovesAction : BonusPremiumAction
    {
        public int addedMoves = 5;
        public override string ActionName => "Add Moves";
        public override string ActionDescription => $"Adds {addedMoves} moves to the move count.";
        public override float BaseCost => 0;
        public override float CostIncrement => 0;
        public override float CostAcceleration => 0;
        public override string VisibilityExplanation => "";
        public override string ClickabilityExplanation => "";
        public override string AvailabilityExplanation => $"Level {unlockedLevel}";
        public override Sprite ActionBarIcon => GameManager.Instance.gfxManager.addMovesIcon;
        public override CurrencyModel CostCurrency => GameManager.Instance.cashCurrency;
        public override ParticleSystem ActionSuccessParticle => GameManager.Instance.gfxManager.addMovesPowerupTrailParticlePrefab;
        public override bool IsVisible()
        {
            // Return true if current level stage is a moves level, otherwise return false. This is to ensure that the add moves action is only visible in moves levels.
            return GameManager.Instance != null &&
                   GameManager.Instance.CurrentLevel != null &&
                   GameManager.Instance.CurrentLevel is LevelScene_Match3Game levelScene &&
                   levelScene.levelLimitType == LevelEditor.LevelLimitType.Moves;
        }
        public override void OnClick()
        {
            CurrentCount--;
            if (ActionSuccessParticle != null)
            {
                Transform addMovesActionTransform = GameManager.Instance.actionBarManager.GetActionBarView(this).transform;
                Transform movesTextTransform = GameManager.Instance.upperPanelUI.timerText.transform;
                ParticleSystem particle = GameObject.Instantiate(ActionSuccessParticle, addMovesActionTransform.position, Quaternion.identity);
                particle.transform.DOMove(movesTextTransform.position, 1f).OnComplete(() => {
                    GameManager.Instance.upperPanelUI.timerText.transform.DOScale(1.22f, 0.2f).SetEase(Ease.Linear).OnComplete(() =>
                    {
                        GameManager.Instance.upperPanelUI.timerText.transform.DOScale(1f, 0.2f).SetEase(Ease.Linear);
                    });
                    GameObject.Destroy(particle.gameObject);
                });
                // You can also add a sound effect here when the particle reaches the target.
            }
            (GameManager.Instance.CurrentLevel as LevelScene_Match3Game).moves += addedMoves;
        }
    }
    [Serializable]
    public class ShuffleAction : BonusPremiumAction
    {

        public override string ActionName => "Shuffle";
        public override string ActionDescription => "Shuffles the board.";
        public override Sprite ActionBarIcon => GameManager.Instance.gfxManager.shuffleActionIcon;
        public override CurrencyModel CostCurrency => GameManager.Instance.premiumCurrency;
        public override ParticleSystem ActionSuccessParticle => GameManager.Instance.gfxManager.shufflePowerupTrailParticlePrefab;
        public override float BaseCost => 0;
        public override float CostIncrement => 0;
        public override float CostAcceleration => 0;
        public override string VisibilityExplanation => "";
        public override string ClickabilityExplanation => "";
        public override string AvailabilityExplanation => $"Level {unlockedLevel}";

        public override bool IsVisible()
        {
            return true;
        }

        public override void OnClick()
        {
            Match3Grid grid = GetMatch3Grid();
            if (grid == null) return;

            CurrentCount--;
            EventManager.TriggerEvent(GameEvent.ACTION_SUCCESSFUL, new EventParam(paramStr: ActionName));

            if (GameManager.Instance != null)
                GameManager.Instance.StartCoroutine(ShuffleRoutine(grid));
        }

        private IEnumerator ShuffleRoutine(Match3Grid grid)
        {
            if (grid == null) yield break;
            yield return grid.StartCoroutine(grid.ShuffleBoardAndResolve());
        }

        private Match3Grid GetMatch3Grid()
        {
            if (!(GameManager.Instance != null && GameManager.Instance.CurrentLevel is LevelScene_Match3Game level))
                return null;

            return level.grid as Match3Grid;
        }

    }

    [Serializable]
    public class BombPlacementAction : BonusPremiumAction
    {
        public override string ActionName => "Bomb Placement";
        public override string ActionDescription => "Places a bomb on the board, then detonates it.";
        public override Sprite ActionBarIcon => GameManager.Instance.gfxManager.bombElementIcon;
        public override CurrencyModel CostCurrency => GameManager.Instance.premiumCurrency;
        public override ParticleSystem ActionSuccessParticle => GameManager.Instance.gfxManager.bombPowerupTrailParticlePrefab;
        public override float BaseCost => 0;
        public override float CostIncrement => 0;
        public override float CostAcceleration => 0;
        public override string VisibilityExplanation => "";
        public override string ClickabilityExplanation => "";
        public override string AvailabilityExplanation => $"Level {unlockedLevel}";

        public override bool IsVisible() => true;

        public override void OnClick()
        {
            Match3GridInputController inputController = GetInputController();
            if (inputController == null) return;

            //CurrentCount--;
            inputController.BeginBombPlacement();
        }

        public IEnumerator BombThrowAnim(Vector3 targetCellLocation)
        {
            Vector3 bombThrowActionPos = GameManager.Instance.actionBarManager.GetActionBarView(this).transform.position;
            bombThrowActionPos.z = targetCellLocation.z; // Ensure the particle is on the same plane as the target cell
            ParticleSystem particle = GameObject.Instantiate(ActionSuccessParticle, bombThrowActionPos, Quaternion.identity);
            yield return particle.transform.DOMove(targetCellLocation, 0.5f).SetEase(Ease.Linear).WaitForCompletion();
            GameObject.Destroy(particle.gameObject);
        }

        private Match3GridInputController GetInputController()
        {
            return UnityEngine.Object.FindObjectOfType<Match3GridInputController>();
        }
    }

    [Serializable]
    public class PlaceDiscoBallAction : BonusPremiumAction
    {
        public override string ActionName => "Place Disco Ball";
        public override string ActionDescription => "Places a disco ball on the board.";
        public override Sprite ActionBarIcon => GameManager.Instance.gfxManager.discoBallElementIcon;
        public override CurrencyModel CostCurrency => GameManager.Instance.premiumCurrency;
        public override ParticleSystem ActionSuccessParticle => GameManager.Instance.gfxManager.discoBallPowerupTrailParticlePrefab;
        public override float BaseCost => 0;
        public override float CostIncrement => 0;
        public override float CostAcceleration => 0;
        public override string VisibilityExplanation => "";
        public override string ClickabilityExplanation => "";
        public override string AvailabilityExplanation => $"Level {unlockedLevel}";

        public override bool IsVisible() => true;

        public override void OnClick()
        {
            Match3GridInputController inputController = GetInputController();
            if (inputController == null) return;

            inputController.BeginDiscoBallPlacement();
        }

        public IEnumerator DiscoBallThrowAnim(Vector3 targetCellLocation)
        {
            Vector3 actionPos = GameManager.Instance.actionBarManager.GetActionBarView(this).transform.position;
            actionPos.z = targetCellLocation.z;
            ParticleSystem particle = GameObject.Instantiate(ActionSuccessParticle, actionPos, Quaternion.identity);
            yield return particle.transform.DOMove(targetCellLocation, 0.5f).SetEase(Ease.Linear).WaitForCompletion();
            GameObject.Destroy(particle.gameObject);
        }

        private Match3GridInputController GetInputController()
        {
            return UnityEngine.Object.FindObjectOfType<Match3GridInputController>();
        }
    }

    [Serializable]
    public class PlaceRocketAction : BonusPremiumAction
    {
        public override string ActionName => "Place Rocket";
        public override string ActionDescription => "Places a rocket on the board";
        public override Sprite ActionBarIcon => GameManager.Instance.gfxManager.rocketElementIcon;
        public override CurrencyModel CostCurrency => GameManager.Instance.premiumCurrency;
        public override ParticleSystem ActionSuccessParticle => GameManager.Instance.gfxManager.rocketPowerupTrailParticlePrefab;
        public override float BaseCost => 0;
        public override float CostIncrement => 0;
        public override float CostAcceleration => 0;
        public override string VisibilityExplanation => "";
        public override string ClickabilityExplanation => "";
        public override string AvailabilityExplanation => $"Level {unlockedLevel}";

        public override bool IsVisible() => true;
        public override void OnClick()
        {
            Match3GridInputController inputController = GetInputController();
            if (inputController == null) return;

            inputController.BeginRocketPlacement();
        }

        public IEnumerator RocketThrowAnim(Vector3 targetCellLocation)
        {
            Vector3 actionPos = GameManager.Instance.actionBarManager.GetActionBarView(this).transform.position;
            actionPos.z = targetCellLocation.z;
            ParticleSystem particle = GameObject.Instantiate(ActionSuccessParticle, actionPos, Quaternion.identity);
            yield return particle.transform.DOMove(targetCellLocation, 0.5f).SetEase(Ease.Linear).WaitForCompletion();
            GameObject.Destroy(particle.gameObject);
        }

        private Match3GridInputController GetInputController()
        {
            return UnityEngine.Object.FindObjectOfType<Match3GridInputController>();
        }
    }
}
