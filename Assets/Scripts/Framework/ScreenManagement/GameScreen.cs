using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine; using Game.EventManagement;
namespace Game
{
    public abstract class GameScreen : MonoBehaviour
    {
        public static bool IsAnyScreenOpen =>
            UnityEngine.Object.FindAnyObjectByType<GameScreen>(FindObjectsInactive.Exclude) != null;

        public abstract Screens ScreenID { get; }
        public Animator animator;
        public string playAnimationName;
        public string closeAnimationName;
        public bool notClosedByClickingOutside;
        public bool doesNotCloseOtherOpenScreens;
        public bool preventsOtherScreensFromOpening;

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
    
}

