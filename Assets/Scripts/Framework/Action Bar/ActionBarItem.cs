using DG.Tweening;
using Game;
using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
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
        public ParticleSystem actionSuccessParticle;
        public int startingCount = 10; // This is only used for the first time the player gets this action. After that, the count will be saved in PlayerPrefs and this value will not be used anymore. This is useful for testing and debugging.

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
        virtual public bool IsClickable()
        {
            if (GameManager.Instance != null && GameManager.Instance.tutorialManager != null && GameManager.Instance.tutorialManager.ShouldDisableActionBar)
                return false;

            return CurrentCount > 0;
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
        [ValueDropdown("GetAllFeatures")]
        [FormerlySerializedAs("unlockableFeature")]
        public UnlockableFeature tiedFeature; // This is used to determine whether the action is unlocked or not. If the feature is unlocked, the action will be available regardless of the level requirement.

        private IEnumerable GetAllFeatures()
        {
            ValueDropdownList<UnlockableFeature> features = new ValueDropdownList<UnlockableFeature>();
            foreach (UnlockableFeature feature in GameManager.Instance.featureTracker.features)
            {
                features.Add(feature.featureName, feature);
            }
            return features;
        }
    }

    [Serializable]
    public class AddTimeAction : BonusPremiumAction
    {
        public int addedTime = 30;

        public override string ActionName => "Add Time";
        public override float BaseCost => 0;
        public override float CostIncrement => 0;
        public override float CostAcceleration => 0;
        public override string VisibilityExplanation => "";
        public override string ClickabilityExplanation => "";
        public override string AvailabilityExplanation => $"Level {unlockedLevel}";

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
            return World.Instance.lastPlayedLevelIndex >= unlockedLevel || (tiedFeature != null && tiedFeature.IsUnlocked(World.Instance.lastPlayedLevelIndex));
        }

        public override void OnClick()
        {


            // Send a particle from the action button to the timer text
            GameManager.Instance.StartCoroutine(AddTimeEffect());

        }

        private IEnumerator AddTimeEffect()
        {
            CurrentCount--;
            if (actionSuccessParticle != null)
            {
                Transform addTimeActionTransform = GameManager.Instance.actionBarManager.GetActionBarView(this).transform;
                Transform timerTextTransform = GameManager.Instance.upperPanelUI.timerText.transform;
                ParticleSystem particle = GameObject.Instantiate(actionSuccessParticle, addTimeActionTransform.position, Quaternion.identity);
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
    public class ShuffleAction : BonusPremiumAction
    {

        public override string ActionName => "Shuffle";
        public override float BaseCost => 0;
        public override float CostIncrement => 0;
        public override float CostAcceleration => 0;
        public override string VisibilityExplanation => "";
        public override string ClickabilityExplanation => "";
        public override string AvailabilityExplanation => $"Level {unlockedLevel}";

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
            return World.Instance.lastPlayedLevelIndex >= unlockedLevel || (tiedFeature != null && tiedFeature.IsUnlocked(World.Instance.lastPlayedLevelIndex));
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
        public override float BaseCost => 0;
        public override float CostIncrement => 0;
        public override float CostAcceleration => 0;
        public override string VisibilityExplanation => "";
        public override string ClickabilityExplanation => "";
        public override string AvailabilityExplanation => $"Level {unlockedLevel}";

        public override int GetCost() => 0;

        public override bool IsVisible() => true;

        public override bool IsAvailable() => World.Instance.lastPlayedLevelIndex >= unlockedLevel || (tiedFeature != null && tiedFeature.IsUnlocked(World.Instance.lastPlayedLevelIndex));

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
            ParticleSystem particle = GameObject.Instantiate(actionSuccessParticle, bombThrowActionPos, Quaternion.identity);
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
        public override float BaseCost => 0;
        public override float CostIncrement => 0;
        public override float CostAcceleration => 0;
        public override string VisibilityExplanation => "";
        public override string ClickabilityExplanation => "";
        public override string AvailabilityExplanation => $"Level {unlockedLevel}";

        public override int GetCost() => 0;

        public override bool IsVisible() => true;

        public override bool IsAvailable() => World.Instance.lastPlayedLevelIndex >= unlockedLevel || (tiedFeature != null && tiedFeature.IsUnlocked(World.Instance.lastPlayedLevelIndex));
        
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
            ParticleSystem particle = GameObject.Instantiate(actionSuccessParticle, actionPos, Quaternion.identity);
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
        public override float BaseCost => 0;
        public override float CostIncrement => 0;
        public override float CostAcceleration => 0;
        public override string VisibilityExplanation => "";
        public override string ClickabilityExplanation => "";
        public override string AvailabilityExplanation => $"Level {unlockedLevel}";

        public override int GetCost() => 0;

        public override bool IsVisible() => true;

        public override bool IsAvailable() => World.Instance.lastPlayedLevelIndex >= unlockedLevel || (tiedFeature != null && tiedFeature.IsUnlocked(World.Instance.lastPlayedLevelIndex));

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
            ParticleSystem particle = GameObject.Instantiate(actionSuccessParticle, actionPos, Quaternion.identity);
            yield return particle.transform.DOMove(targetCellLocation, 0.5f).SetEase(Ease.Linear).WaitForCompletion();
            GameObject.Destroy(particle.gameObject);
        }

        private Match3GridInputController GetInputController()
        {
            return UnityEngine.Object.FindObjectOfType<Match3GridInputController>();
        }
    }
}
