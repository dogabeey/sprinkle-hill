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
            float currentFillAmount = fillImage.fillAmount;

            ObjectiveManager objectiveManager = ObjectiveManager.Instance;
            int totalStages = currentLevel.levelEditors.Count;
            float totalObjectives = objectiveManager.GetTotalInitialObjectives();
            float remainingObjectives = objectiveManager.GetTotalRemainingObjectives();
            float progress = 1f - (remainingObjectives / totalObjectives);
            float stageProgress = currentLevel.GetStageProgress();
            float finalProgress = stageProgress + (progress / totalStages);

            fillImage.DOFillAmount(finalProgress, 0.25f).SetEase(Ease.OutCubic);
        }
    }
}
