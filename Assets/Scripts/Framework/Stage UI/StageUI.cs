using UnityEngine;
using UnityEngine.UI;

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
            LevelScene_Match3Game currentLevel = GameManager.Instance.CurrentLevel as LevelScene_Match3Game;
            // Clear existing indicators
            foreach (Transform child in stageIndicatorContainer)
            {
                Destroy(child.gameObject);
            }
            // Create new indicators based on total stages
            int stageIndex = 0;
            foreach (var stage in currentLevel.levelEditors)
            {
                stageIndex++;
                StageIndicator indicator = Instantiate(stageIndicatorPrefab, stageIndicatorContainer);
                indicator.Init(stage, stageIndex);
            }
        }

        public override void DrawUI()
        {
            fillImage.fillAmount = (float)currentLevel.CurrentStageIndex / currentLevel.levelEditors.Count;
        }
    }
}
