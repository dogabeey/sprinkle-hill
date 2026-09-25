using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine; 
using Game.EventManagement;
using UnityEngine.UI;
using Game.Singleton;

namespace Game
{
    public class ScreenManager : SingletonComponent<ScreenManager>
	{
        public Image backgroundImage; // This is toggled when a screen is open to darken the background.

        public List<GameScreen> screens = new List<GameScreen>();

        private float defaultBGAlpha;

        private IEnumerator Start()
        {
            screens.AddRange(Object.FindObjectsByType<GameScreen>(FindObjectsInactive.Include, FindObjectsSortMode.None));

            defaultBGAlpha = backgroundImage.color.a;
            yield break;
        }

        public void Show(Screens screenID, EventParam eventParam = null)
        {
            GameScreen gameScreen = screens.Find(screen => screen.ScreenID == screenID);
            if (gameScreen == null || IsScreenOpeningPrevented(gameScreen))
                return;

            if(!gameScreen.doesNotCloseOtherOpenScreens)
            {
                CloseAllScreens(false);
            }
            ShowBackground();
            ShowScreen(gameScreen, eventParam);
        }

        private bool IsScreenOpeningPrevented(GameScreen requestedScreen)
        {
            return screens.Exists(screen =>
                screen != null &&
                screen != requestedScreen &&
                screen.gameObject.activeInHierarchy &&
                screen.preventsOtherScreensFromOpening);
        }
        private void ShowBackground()
        {
            backgroundImage.color = new Color(backgroundImage.color.r, backgroundImage.color.g, backgroundImage.color.b, 0);
            backgroundImage.enabled = true;
            backgroundImage.DOFade(defaultBGAlpha, 0.5f);
        }

        public void CloseAllScreens(bool alsoClosePersistentScreens = true) {
            screens.ForEach(screen =>
            {
                if (screen && screen.gameObject.activeSelf) 
                {
                    if((alsoClosePersistentScreens && screen.isPersistent) || !screen.isPersistent)
                    {
                        backgroundImage.DOFade(0, 0.5f);
                        backgroundImage.enabled = false;
                        screen.CloseUI();
                    }
                }
            });
        }
        private static void ShowScreen(GameScreen gameScreen, EventParam eventParam)
        {
            Debug.Log($"[ScreenManager] ShowScreen called for {gameScreen.name} with params: {eventParam?.paramDictionary?.Count ?? 0}");
            gameScreen.gameObject.SetActive(true);
            gameScreen.ResolveParams(eventParam);
            gameScreen.InitUI(eventParam);
            if (gameScreen.animator) gameScreen.animator.SetTrigger(gameScreen.playAnimationName);
            EventManager.TriggerEvent(GameEvent.SCREEN_OPENED, new EventParam(
                paramObj: gameScreen.gameObject,
                paramInt: (int)gameScreen.ScreenID
            ));
        }
    }
}
