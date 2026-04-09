using UnityEngine;
using UnityEngine.UI;
using Sirenix.OdinInspector;
using DG.Tweening;

namespace Game
{
    public class LoadingScreen : UIElement
    {
        public CanvasGroup screenContainer;
        public Image fillBar;

        public override void DrawUI()
        {
        }

        public override void InitUI()
        {
            screenContainer.alpha = 1f;
            screenContainer.blocksRaycasts = false;
            fillBar.fillAmount = 0f;
            fillBar.DOFillAmount(1, GameManager.Instance.constantManager.loadingScreenDuration).OnComplete(() =>

            DOVirtual.Float(1, 0, 0.25f, (float value) =>
            {
                screenContainer.alpha = value;
            }));
            screenContainer.blocksRaycasts = false;
        }
    }

}