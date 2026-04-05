using System.Collections.Generic;
using UnityEngine;


namespace Game
{
    public class FeatureTracker : MonoBehaviour
    {
        public List<UnlockableFeature> features = new List<UnlockableFeature>();

        public UnlockableFeature GetNextLockedFeature(int currentLevelIndex)
        {
            UnlockableFeature nextFeature = null;

            for (int i = 0; i < features.Count; i++)
            {
                UnlockableFeature feature = features[i];
                if (feature == null)
                {
                    continue;
                }

                if (feature.IsUnlocked(currentLevelIndex))
                {
                    continue;
                }

                if (nextFeature == null || feature.unlockedLevelIndex < nextFeature.unlockedLevelIndex)
                {
                    nextFeature = feature;
                }
            }

            return nextFeature;
        }

        public int GetLevelsLeftForNextFeature(int currentLevelIndex)
        {
            UnlockableFeature nextFeature = GetNextLockedFeature(currentLevelIndex);
            if (nextFeature == null)
            {
                return 0;
            }

            return Mathf.Max(0, nextFeature.unlockedLevelIndex - currentLevelIndex);
        }

        public void UnlockFeature(string featureName)
        {
            // Implement your unlocking logic here, such as adding the feature to a player's profile or enabling it in the game.
            Debug.Log($"Feature Unlocked: {featureName}");
        }
    }
    [System.Serializable]
    public class UnlockableFeature
    {
        public string featureName;
        public int unlockedLevelIndex; // The level index at which this feature gets unlocked
        public Sprite icon;

        public bool IsUnlocked(int currentLevelIndex)
        {
            return currentLevelIndex >= unlockedLevelIndex;
        }
    }
}