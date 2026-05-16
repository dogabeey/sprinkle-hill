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
    public abstract class ActionBarItem : IBuyable
    {
        public abstract string ItemName { get; }
        public abstract string ItemDescription { get; }
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
            get => PlayerPrefs.GetInt(ItemName + "_count", startingCount);
            set => PlayerPrefs.SetInt(ItemName + "_count", value);
        }

        public Sprite ActionBarIcon;
        public List<IBuyable.BuyBundle> buyConfig;
        public List<IBuyable.BuyBundle> BuyConfig => buyConfig;
        public int[] BuyChoices => new int[] { 1, 5, 25 };

        public bool TryBuy(IBuyable.BuyBundle buyBundle, GameObject source = null)
        {
            int endCost = buyBundle.GetTotalCost(GetCost());
            if (CostCurrency != null)
            {
                if (CurrencyManager.Instance.GetCurrencyAmount(CostCurrency) >= endCost)
                {
                    CurrencyManager.Instance.AddCurrency(CostCurrency, -endCost);
                    CurrentCount += buyBundle.buyCount;
                    PlayBuyFlyToSourceFeedback(buyBundle, source);

                    if (source != null && source.TryGetComponent(out ActionBarView actionBarView))
                        actionBarView.DrawUI();

                    return true;
                }
                else
                {
                    Debug.LogWarning("Not enough currency to buy " + ItemName);
                    return false;
                }
            }
            else
            {
                Debug.LogWarning("CostCurrency is not defined for " + ItemName);
                return false;
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

        /// <summary>
        /// Determines whether the action is currently selected and ready to be used (e.g., in placement mode).
        /// </summary>
        /// <returns>true if the action is selected; otherwise, false.</returns>
        virtual public bool IsSelected()
        {
            return false;
        }

        private void PlayBuyFlyToSourceFeedback(IBuyable.BuyBundle buyBundle, GameObject source)
        {
            if (buyBundle == null || source == null || buyBundle.buyCount <= 0)
                return;

            Canvas mainCanvas = GameManager.Instance != null ? GameManager.Instance.mainCanvas : null;
            RectTransform canvasRect = mainCanvas != null ? mainCanvas.transform as RectTransform : null;
            if (canvasRect == null)
                return;

            if (!TryGetCanvasPosition(canvasRect, mainCanvas, source.transform, out Vector2 targetCanvasPosition))
                return;

            Vector2 startCanvasPosition = targetCanvasPosition;
            BuyScreenNode[] buyScreenNodes = UnityEngine.Object.FindObjectsOfType<BuyScreenNode>(true);
            for (int i = 0; i < buyScreenNodes.Length; i++)
            {
                BuyScreenNode buyScreenNode = buyScreenNodes[i];
                if (buyScreenNode == null || !ReferenceEquals(buyScreenNode.buyBundle, buyBundle) || buyScreenNode.itemImage == null)
                    continue;

                if (TryGetCanvasPosition(canvasRect, mainCanvas, buyScreenNode.itemImage.rectTransform, out Vector2 buyNodeCanvasPosition))
                    startCanvasPosition = buyNodeCanvasPosition;

                break;
            }

            Sprite feedbackSprite = ActionBarIcon;
            if (feedbackSprite == null)
                return;

            for (int i = 0; i < buyBundle.buyCount; i++)
            {
                GameObject flyObject = new GameObject($"{ItemName}_BuyFlyFeedback_{i}", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
                RectTransform flyRect = flyObject.GetComponent<RectTransform>();
                flyRect.SetParent(canvasRect, false);
                flyRect.anchorMin = new Vector2(0.5f, 0.5f);
                flyRect.anchorMax = new Vector2(0.5f, 0.5f);
                flyRect.pivot = new Vector2(0.5f, 0.5f);
                flyRect.sizeDelta = new Vector2(64f, 64f);
                flyRect.anchoredPosition = startCanvasPosition + UnityEngine.Random.insideUnitCircle * 30f;
                flyRect.localScale = Vector3.one * 3f;

                Image flyImage = flyObject.GetComponent<Image>();
                flyImage.raycastTarget = false;
                flyImage.sprite = feedbackSprite;
                flyImage.preserveAspect = true;

                CanvasGroup canvasGroup = flyObject.GetComponent<CanvasGroup>();
                float delay = i * 0.04f;
                float jumpPower = UnityEngine.Random.Range(30f, 80f);
                Vector2 finalTargetPosition = targetCanvasPosition + UnityEngine.Random.insideUnitCircle * 12f;

                Sequence sequence = DOTween.Sequence();
                sequence.SetDelay(delay);
                sequence.Append(flyRect.DOJumpAnchorPos(finalTargetPosition, jumpPower, 1, 0.45f).SetEase(Ease.OutQuad));
                sequence.Join(flyRect.DOScale(0.55f, 0.45f).SetEase(Ease.InQuad));
                sequence.Join(canvasGroup.DOFade(0f, 0.12f).SetDelay(0.33f));
                sequence.OnComplete(() => UnityEngine.Object.Destroy(flyObject));
            }
        }

        private static bool TryGetCanvasPosition(RectTransform canvasRect, Canvas canvas, Transform target, out Vector2 canvasPosition)
        {
            canvasPosition = default;
            if (canvasRect == null || target == null)
                return false;

            Camera eventCamera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
            Vector3 worldPosition = target is RectTransform rectTransform
                ? rectTransform.TransformPoint(rectTransform.rect.center)
                : target.position;
            Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(eventCamera, worldPosition);
            return RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPoint, eventCamera, out canvasPosition);
        }

    }
    [Serializable]
    public abstract class BoosterBarAction : ActionBarItem
    {
        public Animator preExecutionAnimator; // This is used to play an animation on the action button before executing the action.
        public string animationName; // This is the name of the animation to play on the preExecutionAnimator.
        public int unlockedLevel;
        public int buyCost = 50;
        [FoldoutGroup("Override Settings")]
        public bool overrideCellLocationX;
        [FoldoutGroup("Override Settings")]
        public bool overrideCellLocationY;
        [FoldoutGroup("Override Settings")]
        public int fixedCellLocationX;
        [FoldoutGroup("Override Settings")]
        public int fixedCellLocationY;
        public override bool CostDefinesBuyability => true;
        public override CurrencyModel CostCurrency => GameManager.Instance.cashCurrency;

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
    public class AddTimeAction : BoosterBarAction
    {
        public int addedTime = 30;

        public override string ItemName => "Add Time";
        public override string ItemDescription => $"Adds {addedTime} seconds to the timer.";
        public override float BaseCost => 0;
        public override float CostIncrement => 0;
        public override float CostAcceleration => 0;
        public override string VisibilityExplanation => "";
        public override string ClickabilityExplanation => "";
        public override string AvailabilityExplanation => $"Level {unlockedLevel}";

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
    public class AddMovesAction : BoosterBarAction
    {
        public int addedMoves = 5;
        public override string ItemName => "Add Moves";
        public override string ItemDescription => $"Adds {addedMoves} moves to the move count.";
        public override float BaseCost => 0;
        public override float CostIncrement => 0;
        public override float CostAcceleration => 0;
        public override string VisibilityExplanation => "";
        public override string ClickabilityExplanation => "";
        public override string AvailabilityExplanation => $"Level {unlockedLevel}";
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
    public class ShuffleAction : BoosterBarAction
    {

        public override string ItemName => "Shuffle";
        public override string ItemDescription => "Shuffles the board.";
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
            EventManager.TriggerEvent(GameEvent.ACTION_SUCCESSFUL, new EventParam(paramStr: ItemName));

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
    public class BombPlacementAction : BoosterBarAction
    {
        public override string ItemName => "Bomb Placement";
        public override string ItemDescription => "Places a bomb on the board, then detonates it.";
        public override ParticleSystem ActionSuccessParticle => GameManager.Instance.gfxManager.bombPowerupTrailParticlePrefab;
        public override float BaseCost => 0;
        public override float CostIncrement => 0;
        public override float CostAcceleration => 0;
        public override string VisibilityExplanation => "";
        public override string ClickabilityExplanation => "";
        public override string AvailabilityExplanation => $"Level {unlockedLevel}";

        public override bool IsVisible() => true;

        public override bool IsSelected()
        {
            Match3GridInputController inputController = GetInputController();
            return inputController != null && inputController.IsPlacementActionActive(nameof(BombPlacementAction));
        }

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
    public class PlaceDiscoBallAction : BoosterBarAction
    {
        public override string ItemName => "Place Disco Ball";
        public override string ItemDescription => "Places a disco ball on the board.";
        public override ParticleSystem ActionSuccessParticle => GameManager.Instance.gfxManager.discoBallPowerupTrailParticlePrefab;
        public override float BaseCost => 0;
        public override float CostIncrement => 0;
        public override float CostAcceleration => 0;
        public override string VisibilityExplanation => "";
        public override string ClickabilityExplanation => "";
        public override string AvailabilityExplanation => $"Level {unlockedLevel}";

        public override bool IsVisible() => true;

        public override bool IsSelected()
        {
            Match3GridInputController inputController = GetInputController();
            return inputController != null && inputController.IsPlacementActionActive(nameof(PlaceDiscoBallAction));
        }

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
    public class PlaceRocketAction : BoosterBarAction
    {
        public override string ItemName => "Place Rocket";
        public override string ItemDescription => "Places a rocket on the board";
        public override ParticleSystem ActionSuccessParticle => GameManager.Instance.gfxManager.rocketPowerupTrailParticlePrefab;
        public override float BaseCost => 0;
        public override float CostIncrement => 0;
        public override float CostAcceleration => 0;
        public override string VisibilityExplanation => "";
        public override string ClickabilityExplanation => "";
        public override string AvailabilityExplanation => $"Level {unlockedLevel}";

        public override bool IsVisible() => true;

        public override bool IsSelected()
        {
            Match3GridInputController inputController = GetInputController();
            return inputController != null && inputController.IsPlacementActionActive(nameof(PlaceRocketAction));
        }
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

    [Serializable]
    public class CannonAction : BoosterBarAction
    {
        public override string ItemName => "Cannon";
        public override string ItemDescription => "Destroys the entire column of the clicked cell.";
        public override ParticleSystem ActionSuccessParticle => null;
        public override float BaseCost => 0;
        public override float CostIncrement => 0;
        public override float CostAcceleration => 0;
        public override string VisibilityExplanation => "";
        public override string ClickabilityExplanation => "";
        public override string AvailabilityExplanation => $"Level {unlockedLevel}";

        public override bool IsVisible() => true;

        public override bool IsSelected()
        {
            Match3GridInputController inputController = GetInputController();
            return inputController != null && inputController.IsPlacementActionActive(nameof(CannonAction));
        }

        public override void OnClick()
        {
            Match3GridInputController inputController = GetInputController();
            if (inputController == null) return;

            inputController.BeginCannonPlacement();
        }

        private Match3GridInputController GetInputController()
        {
            return UnityEngine.Object.FindObjectOfType<Match3GridInputController>();
        }
    }

    [Serializable]
    public class HammerAction : BoosterBarAction
    {
        public override string ItemName => "Hammer";
        public override string ItemDescription => "Destroys the clicked cell and adjacent orthogonal cells.";
        public override ParticleSystem ActionSuccessParticle => null;
        public override float BaseCost => 0;
        public override float CostIncrement => 0;
        public override float CostAcceleration => 0;
        public override string VisibilityExplanation => "";
        public override string ClickabilityExplanation => "";
        public override string AvailabilityExplanation => $"Level {unlockedLevel}";

        public override bool IsVisible() => true;

        public override bool IsSelected()
        {
            Match3GridInputController inputController = GetInputController();
            return inputController != null && inputController.IsPlacementActionActive(nameof(HammerAction));
        }

        public override void OnClick()
        {
            Match3GridInputController inputController = GetInputController();
            if (inputController == null) return;

            inputController.BeginHammerPlacement();
        }

        private Match3GridInputController GetInputController()
        {
            return UnityEngine.Object.FindObjectOfType<Match3GridInputController>();
        }
    }
}
