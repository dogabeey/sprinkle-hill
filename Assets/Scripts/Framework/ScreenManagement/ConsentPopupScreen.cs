using UnityEngine.UI;
using UnityEngine; using Game.EventManagement;
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
            AnalyticsManager.Instance.currentConsentState = EndUserConsent.GetConsentState();
            ScreenManager.Instance.CloseAllScreens();
        }
        private void OnDecline()
        {
            EndUserConsent.SetConsentState(new ConsentState
            {
                AnalyticsIntent = ConsentStatus.Denied,
                AdsIntent = ConsentStatus.Denied
            });
            AnalyticsManager.Instance.currentConsentState = EndUserConsent.GetConsentState();
            ScreenManager.Instance.CloseAllScreens();
        }

        public override void ResolveParams(EventParam eventParam)
        {

        }
    }
}
