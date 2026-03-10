using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Game
{
    public class ScreenManager : SingletonComponent<ScreenManager>
    {
        internal List<GameScreen> screens = new List<GameScreen>();

        private IEnumerator Start()
        {
            yield return new WaitForSeconds(0.5f);
            screens.AddRange(FindObjectsOfType<GameScreen>(true));

            //Show(firstScreen);
        }

        private void Update()
        {

        }

        public void Show(GameScreen gameScreen)
        {
            screens.ForEach(screen => {
                if (screen.gameObject.activeSelf)
                {
                    EventManager.TriggerEvent(GameEvent.SCREEN_CLOSED, new EventParam(
                        paramObj: screen.gameObject,
                        paramInt: (int)screen.screenID
                    ));
                }
                screen.gameObject.SetActive(false);
            });
            ShowScreen(gameScreen);
        }

        public void Show(Screens screenID)
        {
            screens.ForEach(screen => {
                if (screen.gameObject.activeSelf)
                {
                    EventManager.TriggerEvent(GameEvent.SCREEN_CLOSED, new EventParam(
                        paramObj: screen.gameObject,
                        paramInt: (int)screen.screenID
                    ));
                }
                screen.gameObject.SetActive(false);
            });
            GameScreen gameScreen = screens.Find(screen => screen.screenID == screenID);
            ShowScreen(gameScreen);
        }
        public void CloseAllScreens()
        {
            screens.ForEach(screen => screen.gameObject.SetActive(false));
        }

        private static void ShowScreen(GameScreen gameScreen)
        {
            gameScreen.gameObject.SetActive(true);
            if (gameScreen.animator) gameScreen.animator.Play(gameScreen.playAnimationName);
            
            EventManager.TriggerEvent(GameEvent.SCREEN_OPENED, new EventParam(
                paramObj: gameScreen.gameObject,
                paramInt: (int)gameScreen.screenID
            ));
        }
    }

}