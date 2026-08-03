using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using TMPro;
using Game.EventManagement;
using Game.Ads;
using UnityEngine.UI;
using System;
using UnityEngine.EventSystems;
using UnityEngine.Events;


namespace Game
{
    public struct PromptButtonParameters
    {
        public const string titleKey = "title";
        public const string descriptionKey = "description";
        public const string buttonActionKey = "button_{0}_action";
        public const string buttonTextKey = "button_{0}_text";
    }

    public class PromptScreen : GameScreen
    {
        [FoldoutGroup("References")] public TMP_Text titleText;
        [FoldoutGroup("References")] public TMP_Text descriptionText;
        [FoldoutGroup("References")] public LayoutGroup buttonsLayoutGroup;
        [FoldoutGroup("Prefabs")] public PromptButton promptButtonPrefab;

        public override Screens ScreenID => Screens.Prompt;

        public static void ShowPrompt(string title, string description, params (string buttonText, UnityAction buttonAction)[] buttons)
        {
            var param = new EventParam
            {
                paramInt = (int)Screens.Prompt,
                paramDictionary = new Dictionary<string, object>()
            };
            param.paramDictionary.Add(PromptButtonParameters.titleKey, title);
            param.paramDictionary.Add(PromptButtonParameters.descriptionKey, description);
            for (int i = 0; i < buttons.Length; i++)
            {
                param.paramDictionary.Add(string.Format(PromptButtonParameters.buttonTextKey, i), buttons[i].buttonText);
                param.paramDictionary.Add(string.Format(PromptButtonParameters.buttonActionKey, i), buttons[i].buttonAction);
            }
            ScreenManager.Instance.Show(Screens.Prompt, param);
        }
        public override void ResolveParams(EventParam eventParam)
        {
            if(eventParam != null)
            {
                if(eventParam.paramDictionary != null)
                {
                    if (eventParam.paramDictionary.TryGetValue(PromptButtonParameters.titleKey, out object title))
                    {
                        titleText.text = title.ToString();
                    }
                    if (eventParam.paramDictionary.TryGetValue(PromptButtonParameters.descriptionKey, out object description))
                    {
                        descriptionText.text = description.ToString();
                    }
                    int i = 0;
                    while(eventParam.paramDictionary.TryGetValue(string.Format(PromptButtonParameters.buttonActionKey, i), out object button_action) 
                    && eventParam.paramDictionary.TryGetValue(string.Format(PromptButtonParameters.buttonTextKey, i), out object button_text))
                    {
                        if(button_action is UnityAction action)
                        {
                            CreateButton(button_text.ToString(), action);
                        }
                        i++;
                    }
                }
            }
        }

        private void CreateButton(string buttonText, UnityAction action)
        {
            var button = Instantiate(promptButtonPrefab, buttonsLayoutGroup.transform);
            button.Init(buttonText, action);
        }
    }
}