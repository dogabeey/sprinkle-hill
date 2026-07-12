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
