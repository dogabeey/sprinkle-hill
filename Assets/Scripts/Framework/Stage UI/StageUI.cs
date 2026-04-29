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
            DOVirtual.Float(currentFillAmount, currentLevel.GetStageProgress(), 0.5f, value => fillImage.fillAmount = value);
        }
    }
}
