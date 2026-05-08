using Sirenix.OdinInspector;
using System;
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
        public string closeAnimationName;
        public bool isPersistent;

        private void OnValidate()
        {
            if (animator == null)
            {
                TryGetComponent(out animator);
            }
        }

        public virtual void InitUI(EventParam eventParam) 
        { 
            ResolveParams(eventParam);
        }
        public abstract void ResolveParams(EventParam eventParam);
        public virtual void CloseUI() {
                if (animator != null && !string.IsNullOrEmpty(closeAnimationName))
                {
                    animator.SetTrigger(closeAnimationName);
                    StartCoroutine(DisableAfterAnimation());
                }
                else
                {
                    gameObject.SetActive(false);
            }
        }

        private IEnumerator DisableAfterAnimation()
        {
            yield return new WaitForSeconds(animator.GetCurrentAnimatorStateInfo(0).length);
            gameObject.SetActive(false);
        }
    }
    public class MainMenuScreen : GameScreen
    {
        public MainMenuScreen()
        {
        }

        public override Screens ScreenID => Screens.MainMenu;

        public override void InitUI(EventParam eventParam)
        {

        }

        public override void ResolveParams(EventParam eventParam)
        {

        }
    }
    
}

