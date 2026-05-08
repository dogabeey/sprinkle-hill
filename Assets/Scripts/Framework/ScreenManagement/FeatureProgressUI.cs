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

        public override void InitUI(EventParam eventParam)
        {
            base.InitUI(eventParam);
            FeatureTracker featureTracker = GameManager.Instance.featureTracker;
            int currentLevelIndex = Mathf.Max(0, World.Instance.lastPlayedLevelIndex);
            UnlockableFeature nextFeature = featureTracker != null ? featureTracker.GetNextLockedFeature(currentLevelIndex) : null;

            if (nextFeature == null)
            {
                if (titleText != null) titleText.text = allFeaturesUnlockedTitle;
                if (featureNameText != null) featureNameText.text = allFeaturesUnlockedMessage;
                if (progressText != null) progressText.text = string.Empty;
                if (featureIconImage != null) featureIconImage.enabled = false;
                if (progressFillImage != null) progressFillImage.fillAmount = 0f;
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
                    float progressTextValue = progress; // Capture the progress value for use in the lambda
                    DOVirtual.Float(0, progressTextValue, 2f, value =>
                    {
                        if (progressText != null)
                        {
                            progressText.text = (value * 100).ToString("F0") + "%";
                        }
                    });
                }
                if (featureIconImage != null)
                {
                    featureIconImage.enabled = nextFeature.icon != null;
                    featureIconImage.sprite = nextFeature.icon;
                }
                if (progressFillImage != null)
                {
                    continueButton.interactable = false;
                    float progressFillValue = 1 - progress; // Capture the progress value for use in the lambda
                    DOVirtual.Float(1, progressFillValue, 2f, value =>
                    {
                        progressFillImage.fillAmount = value;
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
        public override void ResolveParams(EventParam eventParam)
        {

        }
    }
}
