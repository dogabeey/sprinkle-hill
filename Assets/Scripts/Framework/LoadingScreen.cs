using DG.Tweening;
using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game
{
    public class LoadingScreen : UIElement
    {
        public CanvasGroup screenContainer;
        public TMP_Text loadingText;
        public Image fillBar;
        [Header("Settings")]
        public float waitTime;
        public List<string> altLoadingTexts;

        public override void DrawUI()
        {
        }

        private void Awake()
        {
            screenContainer.alpha = 1;
        }
        public override void InitUI()
        {
            screenContainer.alpha = 1f;
            screenContainer.blocksRaycasts = false;

            StartCoroutine(ChangeLoadingTextPeriodically());

            fillBar.fillAmount = 0f;
            fillBar.DOFillAmount(1, waitTime).OnComplete(() =>

            DOVirtual.Float(1, 0, 0.25f, (float value) =>
            {
                screenContainer.alpha = value;
            }).OnComplete(() =>
            {
                screenContainer.blocksRaycasts = false;
                EventManager.TriggerEvent(GameEvent.LOADING_SCREEN_COMPLETE);
            }));
        }

        private IEnumerator ChangeLoadingTextPeriodically()
        {
            while (true)
            { 
                loadingText.text = altLoadingTexts.GetRandomElement();
                yield return new WaitForSeconds(waitTime / 2);
            }
        }
    }

}