using System.Collections;
using UnityEngine;

namespace Game
{
    public class BreakableWall : GridCellController
    {
        [SerializeField] private Animator wallAnimator;
        [SerializeField] private string breakTriggerName = "Break";
        [SerializeField] private float breakAnimationDuration = 0.25f;

        private void Awake()
        {
            if (wallAnimator == null)
            {
                wallAnimator = GetComponent<Animator>();
            }
        }

        public IEnumerator WallBreak()
        {
            if (wallAnimator != null && !string.IsNullOrEmpty(breakTriggerName))
            {
                wallAnimator.SetTrigger(breakTriggerName);

                if (breakAnimationDuration > 0f)
                {
                    yield return new WaitForSeconds(breakAnimationDuration);
                }
            }
        }
    }
}
