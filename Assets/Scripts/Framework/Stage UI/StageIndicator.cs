using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game
{
    public class StageIndicator : MonoBehaviour
    {
        public Image stageImage;
        public TMP_Text stageText;

        internal void Init(LevelEditor stage, int stageIndex)
        {
            if(stage.levelDifficulty == 3)
            {
                if(stageImage) stageImage.sprite = GameManager.Instance.gfxManager.hardLevelIcon;
                if(stageText) stageText.text = "";
            }
            else
            {
                if (stageText) stageText.text = (stageIndex + 1).ToString();
            }
        }
    }
}
