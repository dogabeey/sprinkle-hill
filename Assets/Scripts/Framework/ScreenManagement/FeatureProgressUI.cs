using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game
{
    public class FeatureProgressUI : GameScreen
    {
        public override Screens ScreenID => Screens.FeatureProgress;

        public TMP_Text titleText;
        public TMP_Text featureNameText;
        public TMP_Text progressText;
        public Image featureIconImage;
        public Image progressFillImage;
        public Button continueButton;

        [Header("Text Settings")]
        public string allFeaturesUnlockedTitle = "All Features Unlocked";
        public string allFeaturesUnlockedMessage = "You unlocked all available features.";
        public string progressFormat = "Unlock at level {0} • {1} level left";
        public string progressFormatPlural = "Unlock at level {0} • {1} levels left";

        [Header("References")]
        public FeatureTracker featureTracker;

        public override void InitUI()
        {
            if (featureTracker == null)
            {
                featureTracker = FindAnyObjectByType<FeatureTracker>();
            }

            int currentLevelIndex = Mathf.Max(0, World.Instance.lastPlayedLevelIndex);
            UnlockableFeature nextFeature = featureTracker != null ? featureTracker.GetNextLockedFeature(currentLevelIndex) : null;

            if (nextFeature == null)
            {
                if (titleText != null) titleText.text = allFeaturesUnlockedTitle;
                if (featureNameText != null) featureNameText.text = allFeaturesUnlockedMessage;
                if (progressText != null) progressText.text = string.Empty;
                if (featureIconImage != null) featureIconImage.enabled = false;
                if (progressFillImage != null) progressFillImage.fillAmount = 1f;
            }
            else
            {
                int levelsLeft = featureTracker.GetLevelsLeftForNextFeature(currentLevelIndex);
                int unlockLevelNumber = nextFeature.unlockedLevelIndex + 1;
                int currentLevelNumber = currentLevelIndex + 1;
                float progress = Mathf.Clamp01((float)currentLevelNumber / unlockLevelNumber);

                if (titleText != null) titleText.text = "Next Feature";
                if (featureNameText != null) featureNameText.text = nextFeature.featureName;
                if (progressText != null)
                {
                    progressText.text = (progress * 100).ToString("F0") + "%";
                }
                if (featureIconImage != null)
                {
                    featureIconImage.enabled = nextFeature.icon != null;
                    featureIconImage.sprite = nextFeature.icon;
                }
                if (progressFillImage != null)
                {
                    continueButton.interactable = false;
                    progressFillImage.DOFillAmount(progress, 1f).SetEase(Ease.OutCubic).OnComplete(() =>
                    {
                        continueButton.interactable = true;
                    });
                }
            }

            if (continueButton != null)
            {
                continueButton.onClick.RemoveAllListeners();
                continueButton.onClick.AddListener(() =>
                {
                    ScreenManager.Instance.CloseAllScreens();
                    GameManager.Instance.LoadNextLevel();
                });
            }
        }
    }
}
