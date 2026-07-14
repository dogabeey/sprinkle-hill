using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Game
{
    public class MainMenuPanel : MonoBehaviour
    {

        [SerializeField] private Button currentLevelButton;
        [SerializeField] private TMP_Text currentLevelText;
        [SerializeField] private Button marketButton;
        [SerializeField] private Button leaderboardButton;
        [SerializeField] private Button homeButton;
        [SerializeField] private Button missionsButton;
        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            currentLevelButton.onClick.AddListener(OnCurrentLevelButtonClicked);
            marketButton.onClick.AddListener(OnMarketButtonClicked);
            leaderboardButton.onClick.AddListener(OnLeaderboardButtonClicked);
            homeButton.onClick.AddListener(OnHomeButtonClicked);
            missionsButton.onClick.AddListener(OnMissionsButtonClicked);
        }
        void OnEnable()
        {
            CurrencyManager.Instance.currencyCanvasGroup.alpha = 1f;
            if (currentLevelText)
                currentLevelText.text = $"DAY {GameManager.Instance.CurrentLevelIndex + 1}";
        }
        
        private void OnCurrentLevelButtonClicked()
        {
            GameManager.Instance.ChangeGameState(GameState.Level);
            GameManager.Instance.LoadCurrentLevel();
        }
        private void OnMarketButtonClicked()
        {
            ScreenManager.Instance.Show(Screens.Market);
        }
        private void OnLeaderboardButtonClicked()
        {
            ScreenManager.Instance.Show(Screens.Leaderboard);
        }
        private void OnHomeButtonClicked()
        {
            ScreenManager.Instance.CloseAllNonPersistentScreens();
        }
        private void OnMissionsButtonClicked()
        {
            ScreenManager.Instance.Show(Screens.Missions);
        }
        // Update is called once per frame
        void Update()
        {
            
        }
    }
}
