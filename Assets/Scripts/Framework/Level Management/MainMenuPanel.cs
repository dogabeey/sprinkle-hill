using UnityEngine;
using UnityEngine.UI;

namespace Game
{
    public class MainMenuPanel : GameScreen
    {
        public override Screens ScreenID => Screens.MainMenu;

        [SerializeField] private Button currentLevelButton;
        [SerializeField] private Button marketButton;
        [SerializeField] private Button leaderboardButton;
        [SerializeField] private Button homeButton;
        [SerializeField] private Button missionsButton;
        [SerializeField] private Button settingsButton;
        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            currentLevelButton.onClick.AddListener(OnCurrentLevelButtonClicked);
            marketButton.onClick.AddListener(OnMarketButtonClicked);
            leaderboardButton.onClick.AddListener(OnLeaderboardButtonClicked);
            homeButton.onClick.AddListener(OnHomeButtonClicked);
            missionsButton.onClick.AddListener(OnMissionsButtonClicked);
        }
        
        private void OnCurrentLevelButtonClicked()
        {
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
            ScreenManager.Instance.CloseAllScreens();
        }
        private void OnMissionsButtonClicked()
        {
            ScreenManager.Instance.Show(Screens.Missions);
        }
        // Update is called once per frame
        void Update()
        {
            
        }

        public override void ResolveParams(EventParam eventParam)
        {
            
        }
    }
}
