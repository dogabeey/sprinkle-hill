using UnityEngine;

namespace Game
{
    public class BannerSafeAreaAdjuster : MonoBehaviour
    {
        public RectTransform rectTarget;

        private void OnEnable()
        {
            EventManager.StartListening(GameEvent.BANNER_AD_LOADED, OnBannerAdOpened);
        }
        private void OnDisable()
        {
            EventManager.StopListening(GameEvent.BANNER_AD_LOADED, OnBannerAdOpened);
        }
        private void OnBannerAdOpened(EventParam e)
        {
            float bannerHeight = UnityAdsManager.Instance.bannerHeight;
            // Heighten the safe area by the banner height, so that UI elements will be placed above the banner
            rectTarget.anchoredPosition += new Vector2(0, bannerHeight * 2);
        }
    }
}
