using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine; 
using Game.EventManagement;
using UnityEngine.UI;
using Game.Singleton;

namespace Game
{
    /// <summary>
    /// Action bar manager holds the current action bar items the player can use. The action bar item prefabs added to the list will be 
    /// instantiated and placed in the appropriate positions. This class can be extended to add more functionality to the drawing process.
    /// </summary>
    public class ActionBarManager : SingletonComponent<ActionBarManager>
    { 
        public ActionBarView actionBarViewPrefab;
        [SerializeReference]
        public List<ActionBarItem> actionBarItemList;
        public Transform actionBarParent;
        public Sprite lockedSprite; // Used for not available actions.

        internal List<ActionBarView> actionBarViews = new List<ActionBarView>();
        private readonly Dictionary<ActionBarItem, bool> actionAvailability = new Dictionary<ActionBarItem, bool>();
        private bool hasAvailabilitySnapshot;

        protected override void Awake()
        {
            base.Awake();
            actionBarItemList.ForEach(actionBarItem => actionBarItem.Init());
        }

        private void OnEnable()
        {
            EventManager.StartListening(GameEvent.LOADING_SCREEN_COMPLETE, OnLevelStarted);
        }

        private void OnDisable()
        {
            EventManager.StopListening(GameEvent.LOADING_SCREEN_COMPLETE, OnLevelStarted);
        }

        private void Start()
        {
            DrawUI();
            CacheActionAvailability();
        }

        private void OnLevelStarted(EventParam eventParam)
        {
            Debug.Log($"[FeatureUnlock] LOADING_SCREEN_COMPLETE | Level: {World.Instance?.lastPlayedLevelIndex.ToString() ?? "none"} | Snapshot: {hasAvailabilitySnapshot}");

            if (!hasAvailabilitySnapshot)
            {
                Debug.Log("[FeatureUnlock] Availability snapshot was unavailable; caching now and skipping unlock presentation.");
                CacheActionAvailability();
                return;
            }

            ActionBarItem newlyUnlockedAction = null;
            for (int i = 0; i < actionBarItemList.Count; i++)
            {
                ActionBarItem action = actionBarItemList[i];
                if (action == null)
                    continue;

                bool isAvailable = action.IsAvailable();
                bool wasAvailable = actionAvailability.TryGetValue(action, out bool cachedAvailability) && cachedAvailability;
                actionAvailability[action] = isAvailable;
                bool unlocksOnCurrentLevel = IsUnlockedOnCurrentLevel(action);

                Debug.Log($"[FeatureUnlock] Action: {action.ItemName} | WasAvailable: {wasAvailable} | IsAvailable: {isAvailable} | UnlocksCurrentLevel: {unlocksOnCurrentLevel} | AlreadyShown: {action.HasShownUnlockScreen}");

                if (newlyUnlockedAction == null &&
                    !action.HasShownUnlockScreen &&
                    ((!wasAvailable && isAvailable) || unlocksOnCurrentLevel))
                    newlyUnlockedAction = action;
            }

            RefreshActionBarViews();
            if (newlyUnlockedAction != null)
            {
                Debug.Log($"[FeatureUnlock] Showing feature unlock screen for: {newlyUnlockedAction.ItemName}");
                ShowNewFeatureUnlockScreen(newlyUnlockedAction);
            }
            else
            {
                Debug.Log("[FeatureUnlock] No new action matched the feature unlock criteria.");
            }
        }

        private static bool IsUnlockedOnCurrentLevel(ActionBarItem action)
        {
            return action is BoosterBarAction boosterAction &&
                   World.Instance != null &&
                   World.Instance.lastPlayedLevelIndex == boosterAction.unlockedLevel;
        }

        private void CacheActionAvailability()
        {
            actionAvailability.Clear();
            for (int i = 0; i < actionBarItemList.Count; i++)
            {
                ActionBarItem action = actionBarItemList[i];
                if (action != null)
                    actionAvailability[action] = action.IsAvailable();
            }

            hasAvailabilitySnapshot = true;
        }

        private void RefreshActionBarViews()
        {
            for (int i = 0; i < actionBarViews.Count; i++)
            {
                if (actionBarViews[i] != null)
                    actionBarViews[i].DrawUI();
            }
        }

        private void ShowNewFeatureUnlockScreen(ActionBarItem action)
        {
            if (ScreenManager.Instance == null)
            {
                Debug.LogError($"[FeatureUnlock] Cannot show {action.ItemName}: ScreenManager instance is missing.");
                return;
            }

            GameScreen featureScreen = ScreenManager.Instance.screens.Find(screen => screen != null && screen.ScreenID == Screens.Feature);
            if (featureScreen == null)
            {
                Debug.LogError($"[FeatureUnlock] Cannot show {action.ItemName}: Screens.Feature is not registered. Registered screens: {ScreenManager.Instance.screens.Count}.");
                return;
            }

            Debug.Log($"[FeatureUnlock] Screens.Feature found ({featureScreen.name}); requesting display.");
            action.MarkUnlockScreenShown();
            ScreenManager.Instance.Show(Screens.Feature, new EventParam(new Dictionary<string, object>
            {
                { NewFeatureUnlockScreen.NewFeatureUnlockParameters.featureNameKey, action.textSprite },
                { NewFeatureUnlockScreen.NewFeatureUnlockParameters.featureIconKey, action.actionBarIcon }
            }));
        }

        protected virtual void DrawUI()
        {
            foreach (ActionBarItem actionBarItem in actionBarItemList)
            {
                var actionBar = Instantiate(actionBarViewPrefab, actionBarParent);
                actionBar.Init(actionBarItem);
                actionBarViews.Add(actionBar);
            }
        }

        internal ActionBarView GetActionBarView(ActionBarItem addTimeAction)
        {
            return actionBarViews.FirstOrDefault(actionBarView => actionBarView.actionBarItem == addTimeAction);
        }
    }
}
