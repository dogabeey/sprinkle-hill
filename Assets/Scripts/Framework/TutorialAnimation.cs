using DG.Tweening;
using UnityEngine;

namespace Game
{
    public abstract class TutorialAnimation
    {
        public GameObject tutorialObject;
        public float duration;
        public bool isLoop;

        internal TutorialManager.TutorialStep referenceStep;

        protected GameObject tutorialObjectInstance;

        internal void Initialize(TutorialManager.TutorialStep step, GameObject instance)
        {
            referenceStep = step;
            tutorialObjectInstance = instance;
        }

        public virtual void ClearAnim()
        {
            if (tutorialObjectInstance == null) return;

            tutorialObjectInstance.transform.DOKill();
            Object.Destroy(tutorialObjectInstance);
            tutorialObjectInstance = null;
        }

        public abstract void PlayAnim();
    }

    public class MoveBetweenTwoPoint : TutorialAnimation
    {
        public Vector2 screenPositionOffset;

        public override void PlayAnim()
        {
            if (tutorialObjectInstance == null || referenceStep?.highlightSelector?.HighlightedObjects == null || referenceStep.highlightSelector.HighlightedObjects.Length < 2)
                return;

            Transform startPointTransform = referenceStep.highlightSelector.HighlightedObjects[0].transform;
            Transform endPointTransform = referenceStep.highlightSelector.HighlightedObjects[1].transform;

            // Detect the screen position of the start and end points to determine where the animation should be played
            Vector3 startScreenPos = Camera.main.WorldToScreenPoint(startPointTransform.position) + (Vector3)screenPositionOffset;
            Vector3 endScreenPos = Camera.main.WorldToScreenPoint(endPointTransform.position) + (Vector3)screenPositionOffset;

            tutorialObjectInstance.transform.DOKill();
            tutorialObjectInstance.transform.position = startScreenPos;

            if (isLoop)
            {
                tutorialObjectInstance.transform.DOMove(endScreenPos, duration / 2f).SetLoops(-1, LoopType.Yoyo);
            }
            else
            {
                tutorialObjectInstance.transform.DOMove(endScreenPos, duration / 2f).OnComplete(() =>
                {
                    if (tutorialObjectInstance != null)
                        tutorialObjectInstance.transform.DOMove(startScreenPos, duration / 2f);
                });
            }
        }
    }
}
