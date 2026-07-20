using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Game
{
    /// <summary>
    /// Presents the bonus power-ups that the player has unlocked. Choosing one replaces two
    /// ordinary board elements with that power-up.
    /// </summary>
    public class BoosterSelectionScreen : GameScreen
    {
        [Serializable]
        public class BoosterOption
        {
            [Tooltip("FeatureTracker feature required to show this option, for example 'Bomb' or 'Rocket'.")]
            public string featureName;
            public ElementPowerUpType powerUpType;
            public Button button;
            public Image icon;
        }

        public override Screens ScreenID => Screens.BoosterSelection;

        [Min(1)] public int replacementCount = 2;
        public List<BoosterOption> boosterOptions = new List<BoosterOption>();

        public override void InitUI(EventParam eventParam)
        {
            base.InitUI(eventParam);

            FeatureTracker featureTracker = FindObjectOfType<FeatureTracker>();
            foreach (BoosterOption option in boosterOptions)
            {
                if (option == null || option.button == null)
                    continue;

                UnlockableFeature feature = featureTracker != null
                    ? featureTracker.features.Find(item => item != null && item.featureName == option.featureName)
                    : null;
                bool isUnlocked = feature != null && IsFeatureUnlocked(feature);

                option.button.gameObject.SetActive(isUnlocked);
                option.button.onClick.RemoveAllListeners();
                if (!isUnlocked)
                    continue;

                if (option.icon != null && feature.icon != null)
                    option.icon.sprite = feature.icon;

                ElementPowerUpType selectedPowerUp = option.powerUpType;
                option.button.onClick.AddListener(() => SelectBooster(selectedPowerUp));
            }
        }

        public override void ResolveParams(EventParam eventParam)
        {
        }

        private static bool IsFeatureUnlocked(UnlockableFeature feature)
        {
            return World.Instance != null && feature.IsUnlocked(World.Instance.lastPlayedLevelIndex);
        }

        private void SelectBooster(ElementPowerUpType selectedPowerUp)
        {
            if (selectedPowerUp == ElementPowerUpType.None)
                return;

            Match3Grid grid = (GameManager.Instance.CurrentLevel as LevelScene_Match3Game)?.grid as Match3Grid;
            if (grid == null)
            {
                Debug.LogWarning("Cannot apply bonus booster because there is no active Match3Grid.");
                return;
            }

            int replacedElementCount = grid.ReplaceRandomRegularElementsWithPowerUp(selectedPowerUp, replacementCount);
            if (replacedElementCount > 0)
                ScreenManager.Instance.CloseAllNonPersistentScreens();
        }
    }
}
