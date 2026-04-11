using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
namespace Game
{
    public class SettingScreen : GameScreen
    {
        public override Screens ScreenID => Screens.SettingScreen;

        public Button toggleSettingsButton;
        public GridLayoutGroup layoutGroup;
        public Button restartButton;
        public Toggle musicToggle, sfxToggle, vibrationToggle;

        private void Awake()
        {
            // Toggle the settings button to open the menu
            toggleSettingsButton.onClick.RemoveAllListeners();
            toggleSettingsButton.onClick.AddListener(() =>
            {
                ScreenManager.Instance.Show(ScreenID);
            });

            // Set up toggle listeners
            musicToggle.onValueChanged.AddListener((isOn) =>
            {
                SoundManager.Instance.IsMusicOn = isOn;
            });
            sfxToggle.onValueChanged.AddListener((isOn) =>
            {
                SoundManager.Instance.IsSoundEffectsOn = isOn;
            });
            vibrationToggle.onValueChanged.AddListener((isOn) =>
            {
                SoundManager.Instance.IsVibrationOn = isOn;
            });
        }

        public override void InitUI()
        {
            StartCoroutine(MenuOpenCoroutine());

            musicToggle.isOn = SoundManager.Instance.IsMusicOn;
            sfxToggle.isOn = SoundManager.Instance.IsSoundEffectsOn;
            vibrationToggle.isOn = SoundManager.Instance.IsVibrationOn;

            // Toggle the settings button to close the menu
            toggleSettingsButton.onClick.RemoveAllListeners();
            toggleSettingsButton.onClick.AddListener(() =>
            {
                ScreenManager.Instance.CloseAllScreens();
            });
        }
        public override void CloseUI()
        {
            if(gameObject.activeSelf)
                StartCoroutine(MenuCloseCoroutine());

            // Toggle the settings button to open the menu again
            toggleSettingsButton.onClick.RemoveAllListeners();
            toggleSettingsButton.onClick.AddListener(() =>
            {
                ScreenManager.Instance.Show(ScreenID);
            });

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

