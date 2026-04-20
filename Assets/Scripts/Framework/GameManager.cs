using DG.Tweening;
using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

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
        public EventManager eventManager;
        [FoldoutGroup("Managers")]
        [InlineEditor]
        public SoundManager soundManager;
        [FoldoutGroup("Managers")]
        [InlineEditor]
        public SaveManager saveManager;
        [FoldoutGroup("Managers")]
        [InlineEditor]
        public ConstantManager constantManager;
        [FoldoutGroup("Managers")]
        [InlineEditor]
        public Gfx gfxManager;
        [FoldoutGroup("Managers")]
        [InlineEditor]
        public TutorialManager tutorialManager;
        [FoldoutGroup("Managers")]
        [InlineEditor]
        public ActionBarManager actionBarManager;
        [FoldoutGroup("Managers")]
        [InlineEditor]
        public ScreenManager screenManager;
        [FoldoutGroup("Managers")]
        [InlineEditor]
        public FeatureTracker featureTracker;

        [FoldoutGroup("Settings")]
        public bool showFeatureProgressScreen;
        [FoldoutGroup("Settings")]
        public bool autoShuffleWhenOutOfPossibleMoves;
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

        private bool isAutoShuffleRunning;

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
        public int CurrentLevelIndex => World.Instance.lastPlayedLevelIndex;

        private void OnEnable()
        {
            EventManager.StartListening(GameEvent.LEVEL_COMPLETED, OnLevelCompleted);
            EventManager.StartListening(GameEvent.LEVEL_FAILED, OnLevelFailed);
            EventManager.StartListening(GameEvent.NO_POSSIBLE_MOVES, OnNoPossibleMoves);
        }
        private void OnDisable()
        {
            EventManager.StopListening(GameEvent.LEVEL_COMPLETED, OnLevelCompleted);
            EventManager.StopListening(GameEvent.LEVEL_FAILED, OnLevelFailed);
            EventManager.StopListening(GameEvent.NO_POSSIBLE_MOVES, OnNoPossibleMoves);
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

        private void OnNoPossibleMoves(EventParam param)
        {
            if (!autoShuffleWhenOutOfPossibleMoves || isAutoShuffleRunning)
                return;

            if (!(CurrentLevel is LevelScene_Match3Game levelScene) || levelScene == null || levelScene.isEnded)
                return;

            Match3Grid match3Grid = levelScene.grid as Match3Grid;
            if (match3Grid == null)
                return;

            StartCoroutine(AutoShuffleRoutine(match3Grid));
        }

        private IEnumerator AutoShuffleRoutine(Match3Grid match3Grid)
        {
            isAutoShuffleRunning = true;
            yield return StartCoroutine(match3Grid.ShuffleBoardAndResolve());
            isAutoShuffleRunning = false;
        }

        protected override void Awake()
        {
            base.Awake();

            eventManager.OnInit();
            soundManager.OnInit();
            saveManager.OnInit();

            Application.targetFrameRate = 60;
            CurrentWorld = worlds[0];
        }
        private void Start()
        {

            if (isSequentalLevels)
            {
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
            int sceneIndex = World.Instance.GetSceneIndexForProgressIndex(World.Instance.lastPlayedLevelIndex);
            return sceneIndex >= 0 ? World.Instance.levelScenes[sceneIndex] : null;
        }
        private LevelScene FindNextLevel()
        {
            World.Instance.lastPlayedLevelIndex++;
            int sceneIndex = World.Instance.GetSceneIndexForProgressIndex(World.Instance.lastPlayedLevelIndex);
            return sceneIndex >= 0 ? World.Instance.levelScenes[sceneIndex] : null;
        }

    }
}

