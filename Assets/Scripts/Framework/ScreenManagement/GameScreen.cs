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
        public bool isPersistent;

        private void OnValidate()
        {
            if (animator == null)
            {
                TryGetComponent(out animator);
            }
        }

        public abstract void InitUI();
        public virtual void CloseUI() { }
    }
    public class MainMenuScreen : GameScreen
    {
        public override Screens ScreenID => Screens.MainMenu;

        public override void InitUI()
        {

        }
    }
    
}

