using UnityEngine;
using UnityEngine.UI;

namespace MobileHapticsProFreeEdition
{
    public class ButtonAutoModeOpacity : MonoBehaviour
    {
        public Text buttonText;
        public Image buttonIcon;
        private bool isDimmed = false;

        private void Start()
        {
            // Ensure default opacity is Dim (60) on start
            SetOpacity(60);
        }

        public void ToggleOpacity()
        {
            if (isDimmed)
            {
                SetOpacity(60); // Dim opacity
            }
            else
            {
                SetOpacity(255); // Full opacity
            }
            isDimmed = !isDimmed; // Toggle the state
        }

        private void SetOpacity(byte opacity)
        {
            // Set Text opacity
            Color textColor = buttonText.color;
            textColor.a = opacity / 255f;
            buttonText.color = textColor;

            // Set Icon opacity
            Color iconColor = buttonIcon.color;
            iconColor.a = opacity / 255f;
            buttonIcon.color = iconColor;
        }
    }
}
