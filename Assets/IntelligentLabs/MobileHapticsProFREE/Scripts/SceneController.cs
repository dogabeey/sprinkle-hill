using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

namespace MobileHapticsProFreeEdition
{
    public class SceneController : MonoBehaviour
    {
        [Header("Reference To Animator Component")]
        public GameObject mainCamera;
        private Animator mainCameraAnimator;

        public void LoadHomeScene()
        {
            mainCameraAnimator = mainCamera.GetComponent<Animator>();
            mainCameraAnimator.enabled = true;

            mainCameraAnimator.Play("CameraUpperView");

            // Load "HapticDemoScene_FREE" after 5 seconds
            StartCoroutine(LoadSceneAfterDelay("HapticDemoScene_FREE", 5f));
        }

        public void LoadGameScene()
        {
            // Load "HapticSpaceWaves_FREE" immediately
            StartCoroutine(LoadSceneAfterDelay("HapticSpaceWaves_FREE", 0));
        }

        private IEnumerator LoadSceneAfterDelay(string sceneName, float delay)
        {
            yield return new WaitForSeconds(delay);

            // Check if the scene is in the Build Settings
            if (IsSceneInBuildSettings(sceneName))
            {
                SceneManager.LoadScene(sceneName);
            }
            else
            {
                Debug.LogError($"Scene '{sceneName}' is not added to the Build Settings. Please add it to the Build Settings to avoid this error.");
            }
        }

        private bool IsSceneInBuildSettings(string sceneName)
        {
            // Get all the scenes in the Build Settings
            int sceneCount = SceneManager.sceneCountInBuildSettings;
            for (int i = 0; i < sceneCount; i++)
            {
                string path = SceneUtility.GetScenePathByBuildIndex(i);
                string scene = System.IO.Path.GetFileNameWithoutExtension(path);

                if (scene == sceneName)
                {
                    return true; // Scene is in the Build Settings
                }
            }

            return false; // Scene is not in the Build Settings
        }
    }
}