using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

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

        protected static Vector3 GetScreenPosition(Transform target)
        {
            if (target == null)
                return Vector3.zero;

            RectTransform rectTransform = target as RectTransform;
            if (rectTransform != null)
            {
                Canvas canvas = rectTransform.GetComponentInParent<Canvas>();

                if (canvas != null)
                {
                    switch (canvas.renderMode)
                    {
                        case RenderMode.ScreenSpaceOverlay:
                            return RectTransformUtility.WorldToScreenPoint(null, rectTransform.position);

                        case RenderMode.ScreenSpaceCamera:
                        case RenderMode.WorldSpace:
                            Camera canvasCamera = canvas.worldCamera != null ? canvas.worldCamera : Camera.main;
                            return RectTransformUtility.WorldToScreenPoint(canvasCamera, rectTransform.position);
                    }
                }
            }

            Camera cam = Camera.main;
            return cam != null ? cam.WorldToScreenPoint(target.position) : target.position;
        }
    }

    public class MoveBetweenTwoPoint : TutorialAnimation
    {
        public Vector2 screenPositionOffset;

        public override void PlayAnim()
        {
            if (tutorialObjectInstance == null || referenceStep?.highlightSelector?.HighlightedObjects == null || referenceStep.highlightSelector.HighlightedObjects.Length < 2)
                return;

            DirectiveTextAnim();
            TutorialObjectsAnim();
        }

        private void DirectiveTextAnim()
        {
            var tutorialManager = GameManager.Instance.tutorialManager;
            if (tutorialManager != null && tutorialManager.directiveText != null)
            {
                tutorialManager.directiveText.transform.DOKill();
                tutorialManager.directiveText.transform.localScale = Vector3.one;
                tutorialManager.directiveText.transform.DOScale(Vector3.one * 1.2f, duration / 2f).SetEase(Ease.Linear).SetLoops(-1, LoopType.Yoyo);
            }
        }

        private void TutorialObjectsAnim()
        {
            Transform startPointTransform = referenceStep.highlightSelector.HighlightedObjects[0].transform;
            Transform endPointTransform = referenceStep.highlightSelector.HighlightedObjects[1].transform;

            // Detect the screen position of the start and end points to determine where the animation should be played
            Vector3 startScreenPos = GetScreenPosition(startPointTransform) + (Vector3)screenPositionOffset;
            Vector3 endScreenPos = GetScreenPosition(endPointTransform) + (Vector3)screenPositionOffset;

            tutorialObjectInstance.transform.DOKill();
            tutorialObjectInstance.transform.position = startScreenPos;

            AnimateBetweenPoints(startScreenPos, endScreenPos);
        }

        private void AnimateBetweenPoints(Vector3 startScreenPos, Vector3 endScreenPos)
        {
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
    public class ClickOnFirstHighlightedObject : TutorialAnimation
    {
        public Vector2 screenPositionOffset;

        public override void PlayAnim()
        {
            if (tutorialObjectInstance == null || referenceStep?.highlightSelector?.HighlightedObjects == null)
                return;

            DirectiveTextAnim();
            TutorialObjectsAnim();
        }

        private void DirectiveTextAnim()
        {
            var tutorialManager = GameManager.Instance.tutorialManager;
            if (tutorialManager != null && tutorialManager.directiveText != null)
            {
                tutorialManager.directiveText.transform.DOKill();
                tutorialManager.directiveText.transform.localScale = Vector3.one;
                tutorialManager.directiveText.transform.DOScale(Vector3.one * 1.2f, duration / 2f).SetEase(Ease.Linear).SetLoops(-1, LoopType.Yoyo);
            }
        }

        private void TutorialObjectsAnim()
        {
            Transform startPointTransform = referenceStep.highlightSelector.HighlightedObjects[0].transform;

            // Detect the screen position of the start and end points to determine where the animation should be played
            Vector3 startScreenPos = GetScreenPosition(startPointTransform) + (Vector3)screenPositionOffset;

            tutorialObjectInstance.transform.DOKill();
            tutorialObjectInstance.transform.position = startScreenPos;

            ScaleAnimation();
        }

        private void ScaleAnimation()
        {
            tutorialObjectInstance.transform.DOScale(Vector3.one * 1.1f, duration / 2f).SetEase(Ease.Linear).SetLoops(-1, LoopType.Yoyo);
        }
    }
}
