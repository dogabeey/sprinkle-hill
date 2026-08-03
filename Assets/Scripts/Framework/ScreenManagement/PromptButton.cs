using TMPro;
using UnityEngine;
using UnityEngine.UI;


namespace Game
{
    public class PromptButton : MonoBehaviour
    {
        public Button button;
        public TMP_Text buttonText;

        public void Init(string text, UnityEngine.Events.UnityAction onClick)
        {
            buttonText.text = text;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(onClick);
        }
    }
}