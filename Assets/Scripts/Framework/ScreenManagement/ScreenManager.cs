using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Game
{
    public class ScreenManager : MonoBehaviour
    {
        public static ScreenManager Instance => GameManager.Instance.screenManager;

        public Image backgroundImage; // This is toggled when a screen is open to darken the background.

        internal List<GameScreen> screens = new List<GameScreen>();

        private float defaultBGAlpha;

        private IEnumerator Start()
        {
            yield return new WaitForSeconds(0.2f);
            screens.AddRange(FindObjectsOfType<GameScreen>(true));

            defaultBGAlpha = backgroundImage.color.a;
        }

        private void Update()
        {

        }

        public void Show(GameScreen gameScreen)
        {
            backgroundImage.color = new Color(backgroundImage.color.r, backgroundImage.color.g, backgroundImage.color.b, 0);
            backgroundImage.enabled = true;
            backgroundImage.DOFade(defaultBGAlpha, 0.5f);
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
            backgroundImage.color = new Color(backgroundImage.color.r, backgroundImage.color.g, backgroundImage.color.b, 0);
            backgroundImage.enabled = true;
            backgroundImage.DOFade(defaultBGAlpha, 0.5f);
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
            backgroundImage.DOFade(0, 0.5f);
            backgroundImage.enabled = false;
            screens.ForEach(screen => screen.CloseUI());
        }
        public void CloseAllNonPersistentScreens()
        {
            backgroundImage.DOFade(0, 0.5f);
            backgroundImage.enabled = false;
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