using DG.Tweening;
using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Game;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Game
{
    public interface IManager
    {
        public void OnInit();
        public void OnUpdate();
        public void OnApplicationPause();
        public void OnApplicationQuit();
    }

    public class GameManager : SingletonComponent<GameManager>
    {
        [Header("Managers")]
        [InlineEditor]
        public EventManager eventManager;
        [InlineEditor]
        public SoundManager soundManager;
        [InlineEditor]
        public SaveManager saveManager;
        [InlineEditor]
        public ConstantManager constantManager;

        public List<ElementData> elementDatas;
        [Header("References")]
        public List<World> worlds;
        public Transform levelContainer;
        public ParticleSystem winParticle;
        [Header("Settings")]
        public bool isSequentalLevels = true;

        private World currentWorld;

        public World CurrentWorld
        {
            get
            {
                return currentWorld;
            }
            set
            {

                currentWorld = value;

                EventManager.TriggerEvent(ConstantManager.GameEvents.CURRENT_WORLD_CHANGED, new EventParam());
            }
        }

        private void OnEnable()
        {
            EventManager.StartListening(ConstantManager.GameEvents.LEVEL_COMPLETED, OnLevelCompleted);
            EventManager.StartListening(ConstantManager.GameEvents.LEVEL_FAILED, OnLevelFailed);
        }
        private void OnDisable()
        {
            EventManager.StopListening(ConstantManager.GameEvents.LEVEL_COMPLETED, OnLevelCompleted);
            EventManager.StopListening(ConstantManager.GameEvents.LEVEL_FAILED, OnLevelFailed);
        }
        void OnLevelCompleted(EventParam param)
        {
            winParticle.Play();
            DOVirtual.DelayedCall(1, () =>
            ScreenManager.Instance.Show(Screens.WinScreen));
        }
        void OnLevelFailed(EventParam param)
        {
            DOVirtual.DelayedCall(1, () =>
            ScreenManager.Instance.Show(Screens.LoseScreen));
        }

        protected override void Awake()
        {
            base.Awake();

            eventManager.OnInit();
            soundManager.OnInit();
            saveManager.OnInit();

            Application.targetFrameRate = 60;

            if (isSequentalLevels)
            {
                CurrentWorld = worlds[0];
                if (!FindAnyObjectByType<LevelScene>())
                    LoadCurrentLevel();
            }
        }
        private void Update()
        {
            eventManager.OnUpdate();
            soundManager.OnUpdate();
            saveManager.OnUpdate();
        }
        private void OnApplicationQuit()
        {
            eventManager.OnApplicationQuit();
            soundManager.OnApplicationQuit();
            saveManager.OnApplicationQuit();
        }
        private void OnApplicationPause(bool pause)
        {
            eventManager.OnApplicationPause();
            soundManager.OnApplicationPause();
            saveManager.OnApplicationPause();
        }

        public void LoadLevel(LevelScene levelScene)
        {
            EndCurrentLevel();
            World.Instance.CurrentLevel = Instantiate(levelScene, levelContainer);
        }
        public void LoadCurrentLevel()
        {
            LoadLevel(FindCurrentLevel());
        }
        public void EndCurrentLevel()
        {
            if (World.Instance.CurrentLevel != null)
            {
                Destroy(World.Instance.CurrentLevel.gameObject);
                World.Instance.CurrentLevel = null;

            }
        }
        public void LoadNextLevel()
        {
            if (World.Instance.CurrentLevel != null)
            {
                LoadLevel(FindNextLevel());
            }
        }
        public void ResetCurrentLevel()
        {
            if (World.Instance.CurrentLevel != null)
            {
                LoadLevel(FindCurrentLevel());
            }
        }
        private LevelScene FindCurrentLevel()
        {
            return World.Instance.levelScenes[World.Instance.lastPlayedLevelIndex % World.Instance.levelScenes.Count];
        }
        private LevelScene FindNextLevel()
        {
            World.Instance.lastPlayedLevelIndex++;
            return World.Instance.levelScenes[World.Instance.lastPlayedLevelIndex % World.Instance.levelScenes.Count];
        }

    }
}

