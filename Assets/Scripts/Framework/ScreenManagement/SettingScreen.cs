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
            StartCoroutine(MenuCloseCoroutine());
        }

        private IEnumerator MenuOpenCoroutine()
        {
            DOVirtual.Float(0, 1, 0.3f, (value) =>
            {
                layoutGroup.spacing = Vector2.Lerp(new Vector2(0, 0), new Vector2(0, 50), value);
            }).SetEase(Ease.OutBack);
            musicToggle.gameObject.SetActive(true);
            yield return new WaitForSeconds(0.1f);
            sfxToggle.gameObject.SetActive(true);
            yield return new WaitForSeconds(0.1f);
            vibrationToggle.gameObject.SetActive(true);
            yield return new WaitForSeconds(0.1f);
            restartButton.gameObject.SetActive(true);
        }
        private IEnumerator MenuCloseCoroutine()
        {
            DOVirtual.Float(1, 0, 0.3f, (value) =>
            {
                layoutGroup.spacing = Vector2.Lerp(new Vector2(0, 50), new Vector2(0, 0), value);
            }).SetEase(Ease.InBack);
            restartButton.gameObject.SetActive(false);
            yield return new WaitForSeconds(0.1f);
            vibrationToggle.gameObject.SetActive(false);
            yield return new WaitForSeconds(0.1f);
            sfxToggle.gameObject.SetActive(false);
            yield return new WaitForSeconds(0.1f);
            musicToggle.gameObject.SetActive(false);
        }
    }
}

