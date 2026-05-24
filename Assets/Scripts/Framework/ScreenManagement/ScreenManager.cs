using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine; using Game.EventManagement;
using UnityEngine.UI;

namespace Game
{
    public class ScreenManager : MonoBehaviour
    {
        public static ScreenManager Instance => GameManager.Instance.screenManager;

        public Image backgroundImage; // This is toggled when a screen is open to darken the background.

        public List<GameScreen> screens = new List<GameScreen>();

        private float defaultBGAlpha;

        private IEnumerator Start()
        {
            screens.AddRange(FindObjectsOfType<GameScreen>(true));

            defaultBGAlpha = backgroundImage.color.a;
            yield break;
        }

        private void Update()
        {

        }

        public void Show(GameScreen gameScreen)
        {
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
            GameScreen gameScreen = screens.Find(screen => screen.ScreenID == screenID);
            ShowScreen(gameScreen);
        }

        public void Show(Screens screenID, EventParam eventParam)
        {
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
            GameScreen gameScreen = screens.Find(screen => screen.ScreenID == screenID);
            ShowScreen(gameScreen, eventParam);
        }
        public void CloseAllScreens()
        {
            backgroundImage.DOFade(0, 0.5f);
            backgroundImage.enabled = false;
            screens.ForEach(screen => screen.CloseUI());
        }
        public void CloseAllNonPersistentScreens()
        {
            backgroundImage.DOFade(0, 0.5f);
            backgroundImage.enabled = false;
            screens.ForEach(screen =>
            {
                if (!screen.isPersistent)
                {
                    backgroundImage.DOFade(0, 0.5f);
                    backgroundImage.enabled = false;
                    screen.CloseUI();
                }
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