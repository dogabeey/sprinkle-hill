using UnityEngine.UI;
using UnityEngine;
using Unity.Services.Core;
using UnityEngine.UnityConsent;


namespace Game
{
    public class ConsentPopupScreen : GameScreen
    {
        public override Screens ScreenID => Screens.ConsentPopup;
        public Button acceptButton;
        public Button declineButton;
        private void Start()
        {
            acceptButton.onClick.AddListener(OnAccept);
            declineButton.onClick.AddListener(OnDecline);
        }
        private void OnAccept()
        {
            EndUserConsent.SetConsentState(new ConsentState
            {
                AnalyticsIntent = ConsentStatus.Granted,
                AdsIntent = ConsentStatus.Granted
            });
            ScreenManager.Instance.CloseAllScreens();
        }
        private void OnDecline()
        {
            EndUserConsent.SetConsentState(new ConsentState
            {
                AnalyticsIntent = ConsentStatus.Denied,
                AdsIntent = ConsentStatus.Denied
            });
            ScreenManager.Instance.CloseAllScreens();
        }

        public override void ResolveParams(EventParam eventParam)
        {

        }
    }
}
