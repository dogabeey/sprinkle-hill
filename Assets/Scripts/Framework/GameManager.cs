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
        [FoldoutGroup("Managers")]
        [InlineEditor]
        [FoldoutGroup("Managers")]
        public EventManager eventManager;
        [FoldoutGroup("Managers")]
        [InlineEditor]
        [FoldoutGroup("Managers")]
        public SoundManager soundManager;
        [InlineEditor]
        [FoldoutGroup("Managers")]
        public SaveManager saveManager;
        [InlineEditor]
        [FoldoutGroup("Managers")]
        public ConstantManager constantManager;
        [InlineEditor]
        [FoldoutGroup("Managers")]
        public TutorialManager tutorialManager;
        [InlineEditor]
        [FoldoutGroup("Managers")]
        public ActionBarManager actionBarManager;

        [FoldoutGroup("Settings")]
        public List<ElementData> elementDatas;
        [FoldoutGroup("References")]
        public List<World> worlds;
        [FoldoutGroup("References")]
        public Transform levelContainer;
        [FoldoutGroup("References")]
        public ParticleSystem winParticle;
        [FoldoutGroup("UI References")]
        public UpperPanelUI upperPanelUI;
        [FoldoutGroup("Settings")]
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

                EventManager.TriggerEvent(GameEvent.CURRENT_WORLD_CHANGED, new EventParam());
            }
        }
        public LevelScene CurrentLevel => World.Instance.CurrentLevel;

        private void OnEnable()
        {
            EventManager.StartListening(GameEvent.LEVEL_COMPLETED, OnLevelCompleted);
            EventManager.StartListening(GameEvent.LEVEL_FAILED, OnLevelFailed);
        }
        private void OnDisable()
        {
            EventManager.StopListening(GameEvent.LEVEL_COMPLETED, OnLevelCompleted);
            EventManager.StopListening(GameEvent.LEVEL_FAILED, OnLevelFailed);
        }
        void OnLevelCompleted(EventParam param)
        {
            SoundManager.Instance.Play(ConstantManager.SOUNDS.EFFECTS.LEVEL_COMPLETE);
            winParticle.Play();
            DOVirtual.DelayedCall(1, () =>
            ScreenManager.Instance.Show(Screens.WinScreen));
        }
        void OnLevelFailed(EventParam param)
        {
            SoundManager.Instance.Play(ConstantManager.SOUNDS.EFFECTS.LEVEL_FAILED);
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
                LevelScene foundLevel = FindAnyObjectByType<LevelScene>();
                if (!foundLevel)
                    LoadCurrentLevel();
                else
                {
                    World.Instance.CurrentLevel = foundLevel;
                }
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

