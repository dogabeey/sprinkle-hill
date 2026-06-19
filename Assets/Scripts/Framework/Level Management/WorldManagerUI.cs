using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game
{
    
    public class WorldManagerUI : MonoBehaviour
    {
        public GameManager worldManager;
        public LevelListUI levelListUI;
        public Transform container;
        [AssetsOnly]
        public Button nodePrefab;
        public Button backButton;

        private void Start()
        {
            backButton.onClick.AddListener(() =>
            {
                ScreenManager.Instance.Show(Screens.MainMenu);
            });
            DrawUI();
        }

        public void DrawUI()
        {
            foreach (World world in worldManager.worlds)
            {
                if (world.mainWorld) continue;

                var node = Instantiate(nodePrefab, container);
                node.onClick.AddListener(() =>
                {
                    LoadWorld(world);
                });
                node.GetComponentInChildren<TMP_Text>().text = world.worldName;
            }

        }
        private void LoadWorld(World world)
        {
            GameManager.Instance.CurrentWorld = world;
            ScreenManager.Instance.Show(Screens.LevelList);
        }
    }
    
}