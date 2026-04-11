using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
namespace Game
{
    public class SettingScreen : GameScreen
    {
        public override Screens ScreenID => Screens.SettingScreen;

        public GridLayoutGroup layoutGroup;
        public Button restartButton;
        public Toggle musicToggle, sfxToggle, vibrationToggle;

        public override void InitUI()
        {
            StartCoroutine(MenuOpenCoroutine());
        }
        public override void CloseUI()
        {
            if(gameObject.activeSelf)
                StartCoroutine(MenuCloseCoroutine());
            base.CloseUI();
        }

        private IEnumerator MenuOpenCoroutine()
        {
            musicToggle.gameObject.SetActive(true);
            sfxToggle.gameObject.SetActive(true);
            vibrationToggle.gameObject.SetActive(true);
            restartButton.gameObject.SetActive(true);

            yield return DOVirtual.Float(0, 50, 0.3f, (value) =>
            {
                layoutGroup.spacing = Vector2.one * value;
            }).SetEase(Ease.OutBack).WaitForCompletion();
        }
        private IEnumerator MenuCloseCoroutine()
        {
            yield return DOVirtual.Float(50, 0, 0.3f, (value) =>
            {
                layoutGroup.spacing = Vector2.one * value;
            }).SetEase(Ease.InBack).WaitForCompletion();

            restartButton.gameObject.SetActive(false);
            vibrationToggle.gameObject.SetActive(false);
            sfxToggle.gameObject.SetActive(false);
            musicToggle.gameObject.SetActive(false);
        }
    }
}

