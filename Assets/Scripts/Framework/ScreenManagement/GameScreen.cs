using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Game
{
    public abstract class GameScreen : MonoBehaviour
    {
        public abstract Screens ScreenID { get; }
        public Animator animator;
        public string playAnimationName;

        private void OnValidate()
        {
            if (animator == null)
            {
                TryGetComponent(out animator);
            }
        }

        public abstract void InitUI();
    }
    public class LevelListScreen : GameScreen
    {
        public override Screens ScreenID => Screens.LevelList;

        public override void InitUI()
        {
            
        }
    }
    public class MainMenuScreen : GameScreen
    {
        public override Screens ScreenID => Screens.MainMenu;

        public override void InitUI()
        {

        }
    }
    public class WorldListScreen : GameScreen
    {
        public override Screens ScreenID => Screens.WorldList;

        public override void InitUI()
        {

        }
    }
    
}

