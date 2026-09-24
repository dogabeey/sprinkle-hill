using TMPro;
using UnityEngine.UI;
using UnityEngine;


namespace Game
{
    public class NewFeatureUnlockScreen : GameScreen
    {
        [Header("References")] 
        public Image featureNameTextImage;
        public Image featureIconImage;
        public Button closeButton;
        public struct NewFeatureUnlockParameters
        {
            public const string featureNameKey = "feature_name_image";
            public const string featureIconKey = "feature_icon_image";
        }
        public override Screens ScreenID => Screens.Feature;

        private void Awake()
        {
            if (closeButton != null)
                closeButton.onClick.AddListener(Close);
        }

        private void OnDestroy()
        {
            if (closeButton != null)
                closeButton.onClick.RemoveListener(Close);
        }

        private void Close()
        {
            if (ScreenManager.Instance != null)
                ScreenManager.Instance.CloseAllNonPersistentScreens();
            else
                CloseUI();
        }

        public override void ResolveParams(EventParam eventParam)
        {
            if(eventParam != null)
            {
                if(eventParam.paramDictionary != null)
                {
                    if (eventParam.paramDictionary.TryGetValue(NewFeatureUnlockParameters.featureNameKey, out object featureName))
                    {
                        if(featureName is Sprite sprite)
                        {
                            if (featureNameTextImage != null)
                                featureNameTextImage.sprite = sprite;
                        }
                    }
                    if (eventParam.paramDictionary.TryGetValue(NewFeatureUnlockParameters.featureIconKey, out object featureIcon))
                    {
                        if(featureIcon is Sprite sprite)
                        {
                            if (featureIconImage != null)
                                featureIconImage.sprite = sprite;
                        }
                    }
                }
            }
        }
    }
}
