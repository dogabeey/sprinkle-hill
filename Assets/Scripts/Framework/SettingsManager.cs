using Game.SimpleJSON;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine; using Game.EventManagement;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using Game.Singleton;

namespace Game
{
    public class SettingsManager : SingletonComponent<SettingsManager>
    {
        [Header("References")]
        public VolumeProfile globalVolume;
        [Header("Default Values")]
        public float musicVolume = 1;
        public float sfxVolume = 1;
        [Header("Settings UI")]
        public Slider musicVolumeSlider;
        public Slider sfxVolumeSlider;

        public float MusicVolume
        {
            get => PlayerPrefs.GetFloat("MusicVolume", musicVolume);
            set
            {
                PlayerPrefs.SetFloat("MusicVolume", value);
            }
        }
        public float SfxVolume
        {
            get => PlayerPrefs.GetFloat("SfxVolume", sfxVolume);
            set
            {
                PlayerPrefs.SetFloat("SfxVolume", value);
            }
        }

        protected override void Awake()
        {
            base.Awake();

        }

        private void Start()
        {
            // Add default settings here. We are using Set methods because some of them may contain additional logic.
            SetMusicVolume(MusicVolume);
            SetSFXVolume(SfxVolume);

            // Set UI with the default settings.
            musicVolumeSlider.value = MusicVolume;
            sfxVolumeSlider.value = SfxVolume;
        }

        private void OnEnable()
        {
            // Add listeners to the UI elements.
            musicVolumeSlider.onValueChanged.AddListener(SetMusicVolume);
            sfxVolumeSlider.onValueChanged.AddListener(SetSFXVolume);
        }
        private void OnDisable()
        {
            // Remove listeners to avoid errors.
            musicVolumeSlider.onValueChanged.RemoveAllListeners();
            sfxVolumeSlider.onValueChanged.RemoveAllListeners();
        }

        #region Unity Editor Methods
        public void SetMusicVolume(float value)
        {
            MusicVolume = value;
            SoundManager.Instance.loopingAudioSources.ForEach(p =>
            {
                p.audioSource.volume = MusicVolume;
            });
        }
        public void SetSFXVolume(float value)
        {
            SfxVolume = value;
            SoundManager.Instance.playingAudioSources.ForEach(p =>
            {
                p.audioSource.volume = SfxVolume;
            });
        }
        #endregion
    }

}
