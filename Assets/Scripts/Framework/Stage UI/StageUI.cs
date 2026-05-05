using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace Game
{
    /// <summary>
    /// Handles the display of the level stages.
    /// </summary>
    public class StageUI : UIElement
    {
        public StageIndicator stageIndicatorPrefab;
        public Image fillImage;
        public Transform stageIndicatorContainer;

        internal LevelScene_Match3Game currentLevel;

        private void Start()
        {
            
        }

        public override void InitUI()
        {
            currentLevel = GameManager.Instance.CurrentLevel as LevelScene_Match3Game;
            // Clear existing indicators
            foreach (Transform child in stageIndicatorContainer)
            {
                Destroy(child.gameObject);
            }
            // Create new indicators based on total stages
            int stageIndex = 0;
            foreach (var stage in currentLevel.levelEditors)
            {
                StageIndicator indicator = Instantiate(stageIndicatorPrefab, stageIndicatorContainer);
                indicator.Init(stage, stageIndex);
                stageIndex++;
            }
        }

        public override void DrawUI()
        {
            ObjectiveManager objectiveManager = ObjectiveManager.Instance;
            currentLevel = GameManager.Instance.CurrentLevel as LevelScene_Match3Game;
            int totalStages = currentLevel != null ? currentLevel.levelEditors.Count : 0;
            if (totalStages <= 0)
            {
                fillImage.DOFillAmount(0f, 0.25f).SetEase(Ease.OutCubic);
                return;
            }

            float totalObjectives = objectiveManager != null ? objectiveManager.GetTotalInitialObjectives() : 0f;
            float remainingObjectives = objectiveManager != null ? objectiveManager.GetTotalRemainingObjectives() : 0f;
            float progress = totalObjectives > 0f
                ? 1f - (remainingObjectives / totalObjectives)
                : 0f;
            progress = Mathf.Clamp01(progress);

            float stageProgress = currentLevel.GetStageProgress();
            float finalProgress = stageProgress + (progress / (float)totalStages);
            finalProgress = Mathf.Clamp01(finalProgress);

            fillImage.DOFillAmount(finalProgress, 0.25f).SetEase(Ease.OutCubic);
        }
    }
}
