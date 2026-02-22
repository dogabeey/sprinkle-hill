using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Game
{
    public class GameScreen : SerializedMonoBehaviour
    {
        public Screens screenID;
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

}

