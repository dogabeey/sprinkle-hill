using MobileHapticsProFreeEdition;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace MobileHapticsProFreeEdition
{
    public class HapticButtonManager : MonoBehaviour
    {
        public PlayerController playerController; // Reference to PlayerController 
        public Button topLauncherSButton;
        public Button topLauncherXButton;
        public Button sideLaunchersButton;
        public Button backToCubeButton;

        private Button currentlySelectedButton;

        void Start()
        {
            // Set up listeners for each button
            topLauncherSButton.onClick.AddListener(() => OnButtonPressed(topLauncherSButton));
            topLauncherXButton.onClick.AddListener(() => OnButtonPressed(topLauncherXButton));
            sideLaunchersButton.onClick.AddListener(() => OnButtonPressed(sideLaunchersButton));
            backToCubeButton.onClick.AddListener(() => OnButtonPressed(backToCubeButton));
        }

        void OnButtonPressed(Button pressedButton)
        {
            if (currentlySelectedButton != null)
            {
                SetButtonState(currentlySelectedButton, false);
            }

            currentlySelectedButton = pressedButton;
            SetButtonState(pressedButton, true);

            // TOP LAUNCHER S SETUP (SINGLE)
            if (pressedButton == topLauncherSButton)
            {
                StartCoroutine(DisableButtonTemporarily(topLauncherXButton, 2f));
                StartCoroutine(DisableButtonTemporarily(sideLaunchersButton, 2f));

                if (!playerController.launcherReady)
                {
                    playerController.StartCoroutine(playerController.SetTopLauncherS());
                    return;
                }

                if (playerController.launcherReady && (playerController.isTopLauncherX || playerController.isSideLaunchers))
                {
                    playerController.StartCoroutine(playerController.SetTopLauncherS());
                    return;
                }
            }

            // TOP LAUNCHER X SETUP (DOUBLE)
            if (pressedButton == topLauncherXButton)
            {
                StartCoroutine(DisableButtonTemporarily(topLauncherSButton, 2f));
                StartCoroutine(DisableButtonTemporarily(sideLaunchersButton, 2f));

                if (!playerController.launcherReady)
                {
                    playerController.StartCoroutine(playerController.SetTopLauncherX());
                    return;
                }

                if (playerController.launcherReady && (playerController.isTopLauncherS || playerController.isSideLaunchers))
                {
                    playerController.StartCoroutine(playerController.SetTopLauncherX());
                    return;
                }
            }

            // SIDE LAUNCHERS SETUP
            else if (pressedButton == sideLaunchersButton)
            {
                StartCoroutine(DisableButtonTemporarily(topLauncherSButton, 2f));
                StartCoroutine(DisableButtonTemporarily(topLauncherXButton, 2f));

                if (!playerController.launcherReady)
                {
                    playerController.StartCoroutine(playerController.SetSideLaunchers());
                    return;
                }

                if (playerController.launcherReady && (playerController.isTopLauncherS || playerController.isTopLauncherX))
                {
                    playerController.StartCoroutine(playerController.SetSideLaunchers());
                    return;
                }
            }

            // BACK TO CUBE
            else if (pressedButton == backToCubeButton)
            {
                if (playerController.launcherReady)
                {
                    StartCoroutine(DisableButtonTemporarily(topLauncherSButton, 1f));
                    StartCoroutine(DisableButtonTemporarily(topLauncherXButton, 1f));
                    StartCoroutine(DisableButtonTemporarily(sideLaunchersButton, 1f));
                    playerController.StartCoroutine(playerController.BackToCube());
                }
            }
        }

        void SetButtonState(Button button, bool isPressed)
        {
            ColorBlock colors = button.colors;

            if (isPressed)
            {
                // Change to "PRESSED" state with color FFFFC9
                colors.normalColor = new Color32(252, 255, 201, 255); // FFFFC9
                colors.selectedColor = new Color32(252, 255, 201, 255);
            }
            else
            {
                // Change back to the normal state
                colors.normalColor = Color.white; // Default color
                colors.selectedColor = Color.white;
            }
            button.colors = colors;
        }

        private IEnumerator DisableButtonTemporarily(Button buttonToDisable, float duration)
        {
            buttonToDisable.interactable = false;
            yield return new WaitForSeconds(duration);
            buttonToDisable.interactable = true;
        }
    }
}