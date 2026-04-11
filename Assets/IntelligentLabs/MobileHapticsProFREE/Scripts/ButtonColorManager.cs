using UnityEngine;
using UnityEngine.UI;

namespace MobileHapticsProFreeEdition
{
    public class ButtonColorManager : MonoBehaviour
    {
        public Button[] buttons; // Array to hold all buttons
        public Color imageColor; // Color for the image and icon on click
        public Color textColor;  // Color for the text on click

        private Image[] buttonIcons;  // Array to hold the icons (Images under buttons)
        private Text[] buttonTexts;   // Array to hold the texts
        private Color originalImageColor;
        private Color originalTextColor;

        void Start()
        {
            buttonIcons = new Image[buttons.Length];
            buttonTexts = new Text[buttons.Length];

            // Store references to the Image (icon) and Text components, and save their original colors
            for (int i = 0; i < buttons.Length; i++)
            {
                buttonIcons[i] = buttons[i].transform.Find("icon").GetComponent<Image>();
                buttonTexts[i] = buttons[i].transform.Find("Text").GetComponent<Text>();

                if (i == 0) // Assuming all buttons have the same original color, so we just take the color from the first button
                {
                    originalImageColor = buttonIcons[i].color;
                    originalTextColor = buttonTexts[i].color;
                }

                // Add listener to handle the button click
                int index = i; // Store the index in a local variable to avoid closure issue
                buttons[i].onClick.AddListener(() => OnButtonClicked(index));
            }
        }

        void OnButtonClicked(int index)
        {
            // Reset all buttons' colors
            for (int i = 0; i < buttons.Length; i++)
            {
                buttonIcons[i].color = originalImageColor;
                buttonTexts[i].color = originalTextColor;
            }

            // Change the color of the clicked button's icon and text
            buttonIcons[index].color = imageColor;
            buttonTexts[index].color = textColor;
        }
    }
}