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

            if (stageText) stageText.text = (stageIndex + 1).ToString();
        }
    }
}
