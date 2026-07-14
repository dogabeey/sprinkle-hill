using DG.Tweening;
using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine; using Game.EventManagement;
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
            ToggleLoadingScreen(false);
        }
        private void ToggleLoadingScreen(bool isActive)
        {
            screenContainer.alpha = isActive ? 1f : 0f;
            screenContainer.blocksRaycasts = isActive;
        }
        public override void InitUI()
        {
            ToggleLoadingScreen(true);

            StartCoroutine(ChangeLoadingTextPeriodically());

            fillBar.fillAmount = 0f;
            fillBar.DOFillAmount(1, waitTime).OnComplete(() =>

            DOVirtual.Float(1, 0, 0.25f, (float value) =>
            {
                screenContainer.alpha = value;
            }).OnComplete(() =>
            {
                ToggleLoadingScreen(false);
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