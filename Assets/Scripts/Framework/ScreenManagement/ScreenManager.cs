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
            screens.AddRange(Object.FindObjectsByType<GameScreen>(FindObjectsSortMode.None));

            defaultBGAlpha = backgroundImage.color.a;
            yield break;
        }

        private void Update()
        {

        }

        public void Show(GameScreen gameScreen)
        {
            if (gameScreen == null || IsScreenOpeningPrevented(gameScreen))
                return;

            backgroundImage.color = new Color(backgroundImage.color.r, backgroundImage.color.g, backgroundImage.color.b, 0);
            backgroundImage.enabled = true;
            backgroundImage.DOFade(defaultBGAlpha, 0.5f);
            screens.ForEach(screen =>
            {
                if (screen.gameObject.activeSelf)
                {
                    EventManager.TriggerEvent(GameEvent.SCREEN_CLOSED, new EventParam(
                        paramObj: screen.gameObject,
                        paramInt: (int)screen.ScreenID
                    ));
                }
                screen.gameObject.SetActive(false);
            });
            ShowScreen(gameScreen);
        }

        public void Show(Screens screenID)
        {
            GameScreen gameScreen = screens.Find(screen => screen.ScreenID == screenID);
            if (gameScreen == null || IsScreenOpeningPrevented(gameScreen))
                return;

            if(gameScreen.doesNotCloseOtherOpenScreens)
            {
            CloseAllScreens();
            }
            else
            {
                CloseAllNonPersistentScreens();
            }
            ShowBackground();
            ShowScreen(gameScreen);
        }

        public void Show(Screens screenID, EventParam eventParam)
        {
            GameScreen gameScreen = screens.Find(screen => screen.ScreenID == screenID);
            if (gameScreen == null || IsScreenOpeningPrevented(gameScreen))
                return;

            CloseAllScreens();
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

        public void CloseAllNonPersistentScreens()
        {
            screens.ForEach(screen =>
            {
                if (screen && screen.gameObject && !screen.notClosedByClickingOutside && screen.gameObject.activeSelf)
                {
                    backgroundImage.DOFade(0, 0.5f);
                    backgroundImage.enabled = false;
                    screen.CloseUI();
                }
            });
        }
        public void CloseAllScreens() {
            backgroundImage.DOFade(0, 0.5f);
            backgroundImage.enabled = false;
            screens.ForEach(screen =>
            {
                if (screen) screen.CloseUI();
            });
        }

        private static void ShowScreen(GameScreen gameScreen)
        {
            gameScreen.gameObject.SetActive(true);
            gameScreen.InitUI(new EventParam());
            if (gameScreen.animator) gameScreen.animator.SetTrigger(gameScreen.playAnimationName);

            EventManager.TriggerEvent(GameEvent.SCREEN_OPENED, new EventParam(
                paramObj: gameScreen.gameObject,
                paramInt: (int)gameScreen.ScreenID
            ));
        }
        private static void ShowScreen(GameScreen gameScreen, EventParam eventParam)
        {
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
