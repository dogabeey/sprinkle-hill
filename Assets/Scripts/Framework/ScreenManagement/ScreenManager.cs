using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Game
{
    public class ScreenManager : SingletonComponent<ScreenManager>
    {
        public Image backgroundMask; // This is toggled when a screen is open to darken the background.

        internal List<GameScreen> screens = new List<GameScreen>();

        private float defaultBGAlpha;

        private IEnumerator Start()
        {
            yield return new WaitForSeconds(0.5f);
            screens.AddRange(FindObjectsOfType<GameScreen>(true));

            defaultBGAlpha = backgroundMask.color.a;
        }

        private void Update()
        {

        }

        public void Show(GameScreen gameScreen)
        {
            backgroundMask.enabled = true;
            screens.ForEach(screen => {
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
            backgroundMask.color = new Color(backgroundMask.color.r, backgroundMask.color.g, backgroundMask.color.b, 0);
            backgroundMask.enabled = true;
            backgroundMask.DOFade(defaultBGAlpha, 0.5f);
            screens.ForEach(screen => {
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
        public void CloseAllScreens()
        {
            backgroundMask.DOFade(0, 0.5f);
            backgroundMask.enabled = false;
            screens.ForEach(screen => screen.gameObject.SetActive(false));
        }
        public void CloseAllNonPersistentScreens()
        {
            backgroundMask.DOFade(0, 0.5f);
            backgroundMask.enabled = false;
            screens.ForEach(screen => {
                if (!screen.isPersistent) screen.gameObject.SetActive(false);
            });
        }

        private static void ShowScreen(GameScreen gameScreen)
        {
            gameScreen.gameObject.SetActive(true);
            gameScreen.InitUI();
            if (gameScreen.animator) gameScreen.animator.Play(gameScreen.playAnimationName);
            
            EventManager.TriggerEvent(GameEvent.SCREEN_OPENED, new EventParam(
                paramObj: gameScreen.gameObject,
                paramInt: (int)gameScreen.ScreenID
            ));
        }
    }

}