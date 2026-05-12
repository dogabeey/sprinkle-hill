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

    public class GameManager : SingletonComponent<GameManager>
    {
        [FoldoutGroup("Managers")]
        public EventManager eventManager;
        [FoldoutGroup("Managers")]
        public SoundManager soundManager;
        [FoldoutGroup("Managers")]
        public SaveManager saveManager;
        [FoldoutGroup("Managers")]
        public ConstantManager constantManager;
        [FoldoutGroup("Managers")]
        public Gfx gfxManager;
        [FoldoutGroup("Managers")]
        public TutorialManager tutorialManager;
        [FoldoutGroup("Managers")]
        public ActionBarManager actionBarManager;
        [FoldoutGroup("Managers")]
        public ScreenManager screenManager;
        [FoldoutGroup("Managers")]
        public FeatureTracker featureTracker;
        [FoldoutGroup("Managers")]
        public PoolingManager poolingManager;

        [FoldoutGroup("Settings")]
        public bool showFeatureProgressScreen;
        [FoldoutGroup("Settings")]
        public CurrencyModel cashCurrency;
        [FoldoutGroup("Settings")]
        public CurrencyModel premiumCurrency;
        [FoldoutGroup("Settings")]
        public bool autoShuffleWhenOutOfPossibleMoves;
        [FoldoutGroup("Element Definitions")]
        public ElementData bombElementData;
        [FoldoutGroup("Element Definitions")]
        public ElementData rocketElementData;
        [FoldoutGroup("Element Definitions")]
        public ElementData propellerElementData;
        [FoldoutGroup("Element Definitions")]
        public ElementData discoBallElementData;
        [FoldoutGroup("Element Definitions")]
        public ElementData cauldronElementData;
        [FoldoutGroup("Element Definitions")]
        public ElementData garbageBagElementData;
        [FoldoutGroup("Element Definitions")]
        public ElementData powerGeneratorElementData;
        [FoldoutGroup("Element Definitions")]
        public ElementData powerOutletElementData;
        [FoldoutGroup("Cell Features")]
        public WaferFeature waferFeature;
        [FoldoutGroup("Cell Features")]
        public GlassFeature glassFeature;
        [FoldoutGroup("Cell Features")]
        public ElectricField electricField;
        [FoldoutGroup("Cell Features")]
        public LockedAreaFeature lockedAreaFeature;
        [FoldoutGroup("References")]
        public List<World> worlds;
        [FoldoutGroup("References")]
        public Transform levelContainer;
        [FoldoutGroup("References")]
        public ParticleSystem[] winParticle;
        [FoldoutGroup("UI References")]
        public Canvas mainCanvas;
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
            foreach (var particle in winParticle)            
            {
                particle.Play();
            }
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

            Application.targetFrameRate = 60;
            CurrentWorld = worlds[0];
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

