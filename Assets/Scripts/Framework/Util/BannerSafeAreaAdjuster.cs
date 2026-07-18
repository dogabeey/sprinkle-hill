using UnityEngine; 
using Game.EventManagement;

namespace Game
{
    public class BannerSafeAreaAdjuster : MonoBehaviour
    {
        public RectTransform rectTarget;

        private Vector2 originalAnchoredPosition;

        private void OnEnable()
        {
            originalAnchoredPosition = rectTarget.anchoredPosition;
            EventManager.StartListening(GameEvent.BANNER_AD_LOADED, OnBannerAdOpened);
        }
        private void OnDisable()
        {
            EventManager.StopListening(GameEvent.BANNER_AD_LOADED, OnBannerAdOpened);
        }
        private void OnBannerAdOpened(EventParam e)
        {
            float bannerHeight = e.paramFloat;
			// Heighten the safe area by the banner height, so that UI elements will be placed above the banner
            if (originalAnchoredPosition == Vector2.zero)
                originalAnchoredPosition = rectTarget.anchoredPosition;
			rectTarget.anchoredPosition = originalAnchoredPosition + new Vector2(0, bannerHeight);
        }
    }
}
