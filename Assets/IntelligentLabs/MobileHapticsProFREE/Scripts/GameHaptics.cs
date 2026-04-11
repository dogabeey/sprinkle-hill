using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace MobileHapticsProFreeEdition
{
    public class GameHaptics : MonoBehaviour
    {
        private string ultimateEditionURL = "https://assetstore.unity.com/packages/tools/game-toolkits/mobile-haptics-pro-ultimate-edition-299877";

        public void Select()
        {
            TapticWave.TriggerHaptic(HapticModes.Select);
        }

        public void Confirm()
        {
            TapticWave.TriggerHaptic(HapticModes.Confirm);
        }

        public void Alert()
        {
            TapticWave.TriggerHaptic(HapticModes.Alert);
        }

        public void Failure()
        {
            TapticWave.TriggerHaptic(HapticModes.Failure);
        }

        // Ultimate Edition Haptics: Refer to Store Page
        public void VerySoftTap()
        {
            RedirectToUltimateStorePage();
        }

        public void SoftTap()
        {
            RedirectToUltimateStorePage();
        }

        public void MediumTap()
        {
            RedirectToUltimateStorePage();
        }

        public void HardTap()
        {
            RedirectToUltimateStorePage();
        }

        public void HalfSecondWave()
        {
            RedirectToUltimateStorePage();
        }

        public void OneSecondWave()
        {
            RedirectToUltimateStorePage();
        }

        public void TwoSecondsWave()
        {
            RedirectToUltimateStorePage();
        }

        private void RedirectToUltimateStorePage()
        {
            Application.OpenURL(ultimateEditionURL);
        }
    }
}
