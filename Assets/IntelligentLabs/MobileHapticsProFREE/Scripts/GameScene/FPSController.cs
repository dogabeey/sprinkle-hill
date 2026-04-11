using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MobileHapticsProFreeEdition
{
    public class FPSController : MonoBehaviour
    {
        void Start()
        {
            // Attempt to set the target frame rate to 120 FPS
            Application.targetFrameRate = 120;

            // Check if the device supports 120 FPS
            if (Screen.currentResolution.refreshRateRatio.numerator < 120)
            {
                // If not, fall back to 60 FPS
                Application.targetFrameRate = 60;
            }

            Debug.Log("Target Frame Rate: " + Application.targetFrameRate);

            // Lock screen orientation to portrait
            Screen.orientation = ScreenOrientation.Portrait;
        }
    }
}