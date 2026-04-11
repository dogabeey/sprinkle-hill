using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Game
{
    public class DebugManager : MonoBehaviour
    {
        public GameObject coinSource;

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.W))
            {
                GameManager.Instance.CurrentLevel.CompleteLevel();
            }
            if (Input.GetKeyDown(KeyCode.F))
            {
                GameManager.Instance.CurrentLevel.FailLevel("You are out of time.\n\nDon't worry, you'll get them next time.");
            }
            if (Input.GetKeyDown(KeyCode.R))
            {
                PlayerPrefs.DeleteAll();
#if UNITY_EDITOR
                SaveManager.ClearAllSaves();
#endif
            }
            if(Input.GetKeyDown(KeyCode.L))
            {
                GameManager.Instance.ResetCurrentLevel();
            }
            if(Input.GetKeyDown(KeyCode.A))
            {
                if(Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
                {
                    UnityAdsManager.Instance.ShowRewardedAd();
                }
                else
                {
                    UnityAdsManager.Instance.ShowAd();
                }
            }
        }
    }
}
