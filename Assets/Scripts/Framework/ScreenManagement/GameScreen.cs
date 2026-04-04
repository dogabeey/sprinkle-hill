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
    }
    public class LevelListScreen : GameScreen
    {
        public override Screens ScreenID => Screens.LevelList;
    }
    public class MainMenuScreen : GameScreen
    {
        public override Screens ScreenID => Screens.MainMenu;
    }
    public class WorldListScreen : GameScreen
    {
        public override Screens ScreenID => Screens.WorldList;
    }
    
}

