using DG.Tweening;
using UnityEngine; using Game.EventManagement;
using UnityEngine.UI;

namespace Game
{
    [System.Serializable]
    public abstract class TutorialAnimation
    {
        public RectTransform tutorialObject;
        public Vector2 screenPositionOffset;
        public float duration;
        public bool isLoop;

        internal TutorialStep referenceStep;

        protected RectTransform tutorialObjectInstance;

        internal void Initialize(TutorialStep step, RectTransform instance)
        {
            referenceStep = step;
            tutorialObjectInstance = instance;
        }

        public virtual void ClearAnim()
        {
            if (tutorialObjectInstance == null) return;
             
            tutorialObjectInstance.DOKill();
            Object.Destroy(tutorialObjectInstance.gameObject);
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

        protected static Vector3 GetScreenPosition(Vector3 worldPosition)
        {
            Camera cam = Camera.main;
            return cam != null ? cam.WorldToScreenPoint(worldPosition) : worldPosition;
        }

        protected Vector2 ScreenToAnchoredPosition(Vector2 screenPosition)
        {
            if (tutorialObjectInstance == null)
                return screenPosition;

            RectTransform parentRect = tutorialObjectInstance.parent as RectTransform;
            if (parentRect == null)
                return screenPosition;

            Camera uiCamera = GetCanvasCamera(parentRect);
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, screenPosition, uiCamera, out Vector2 anchoredPos))
                return anchoredPos;

            return screenPosition;
        }

        protected Vector2 GetAnchoredPosition(Transform target)
        {
            Vector2 screenPos = GetScreenPosition(target);
            return ScreenToAnchoredPosition(screenPos + screenPositionOffset);
        }

        private static Camera GetCanvasCamera(RectTransform rectTransform)
        {
            Canvas canvas = rectTransform.GetComponentInParent<Canvas>();
            if (canvas == null)
                return Camera.main;

            if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
                return null;

            return canvas.worldCamera != null ? canvas.worldCamera : Camera.main;
        }
    }

    [System.Serializable]
    public class MoveBetweenTwoPoint : TutorialAnimation
    {

        public override void PlayAnim()
        {
            GameObject[] highlightedObjects = referenceStep != null ? referenceStep.GetHighlightedObjects() : null;
            if (tutorialObjectInstance == null || highlightedObjects == null || highlightedObjects.Length < 2)
                return;

            DirectiveTextAnim();
            TutorialObjectsAnim();
        } 

        private void DirectiveTextAnim()
        {
            var tutorialManager = TutorialManager.Instance;
            if (tutorialManager != null && tutorialManager.directiveText != null)
            {
                tutorialManager.directiveText.transform.DOKill();
                tutorialManager.directiveText.transform.localScale = Vector3.one;
                tutorialManager.directiveText.transform.DOScale(Vector3.one * 1.2f, duration / 2f).SetEase(Ease.Linear).SetLoops(-1, LoopType.Yoyo);
            }
        }

        private void TutorialObjectsAnim()
        {
            GameObject[] highlightedObjects = referenceStep != null ? referenceStep.GetHighlightedObjects() : null;
            if (highlightedObjects == null || highlightedObjects.Length < 2 || highlightedObjects[0] == null || highlightedObjects[1] == null)
                return;

            Transform startPointTransform = highlightedObjects[0].transform;
            Transform endPointTransform = highlightedObjects[1].transform;

            // Detect the screen position of the start and end points to determine where the animation should be played
            Vector2 startAnchoredPos = GetAnchoredPosition(startPointTransform);
            Vector2 endAnchoredPos = GetAnchoredPosition(endPointTransform);

            tutorialObjectInstance.DOKill();
            tutorialObjectInstance.anchoredPosition = startAnchoredPos;

            AnimateBetweenPoints(startAnchoredPos, endAnchoredPos);
        }

        private void AnimateBetweenPoints(Vector2 startAnchoredPos, Vector2 endAnchoredPos)
        {
            if (isLoop)
            {
                tutorialObjectInstance.DOAnchorPos(endAnchoredPos, duration / 2f).SetLoops(-1, LoopType.Yoyo);
            }
            else
            {
                tutorialObjectInstance.DOAnchorPos(endAnchoredPos, duration / 2f).OnComplete(() =>
                {
                    if (tutorialObjectInstance != null)
                        tutorialObjectInstance.DOAnchorPos(startAnchoredPos, duration / 2f);
                });
            }
        }
    }
    [System.Serializable]
    public class MoveBetweenTwoCoordinates : TutorialAnimation
    {
        public Vector2Int startCoordinate;
        public Vector2Int endCoordinate;

        public override void PlayAnim()
        {
            if (tutorialObjectInstance == null)
                return;

            if (!TryGetAnchoredPositionFromCoordinate(startCoordinate, out Vector2 startAnchoredPos) ||
                !TryGetAnchoredPositionFromCoordinate(endCoordinate, out Vector2 endAnchoredPos))
            {
                return;
            }

            tutorialObjectInstance.DOKill();
            tutorialObjectInstance.anchoredPosition = startAnchoredPos;

            AnimateBetweenPoints(startAnchoredPos, endAnchoredPos);
        }

        private bool TryGetAnchoredPositionFromCoordinate(Vector2Int coordinate, out Vector2 anchoredPosition)
        {
            anchoredPosition = default;

            Match3Grid grid = (GameManager.Instance?.CurrentLevel as LevelScene_Match3Game)?.grid as Match3Grid;
            GridCellController cell = grid != null ? grid.GetCellControllerAt(coordinate) : null;
            if (cell == null)
                return false;

            Vector3 cellCenter = cell.gridSprite != null
                ? cell.gridSprite.bounds.center
                : cell.transform.position;
            Vector2 screenPosition = GetScreenPosition(cellCenter);
            anchoredPosition = ScreenToAnchoredPosition(screenPosition + screenPositionOffset);
            return true;
        }

        private void AnimateBetweenPoints(Vector2 startAnchoredPos, Vector2 endAnchoredPos)
        {
            if (isLoop)
            {
                tutorialObjectInstance.DOAnchorPos(endAnchoredPos, duration / 2f).SetLoops(-1, LoopType.Yoyo);
            }
            else
            {
                tutorialObjectInstance.DOAnchorPos(endAnchoredPos, duration / 2f).OnComplete(() =>
                {
                    if (tutorialObjectInstance != null)
                        tutorialObjectInstance.DOAnchorPos(startAnchoredPos, duration / 2f);
                });
            }
        }
    }
    [System.Serializable]
    public class ClickOnFirstHighlightedObject : TutorialAnimation
    {

        public override void PlayAnim()
        {
            GameObject[] highlightedObjects = referenceStep != null ? referenceStep.GetHighlightedObjects() : null;
            if (tutorialObjectInstance == null || highlightedObjects == null || highlightedObjects.Length == 0)
                return;

            DirectiveTextAnim();
            TutorialObjectsAnim();
        }

        private void DirectiveTextAnim()
        {
            var tutorialManager = TutorialManager.Instance;
            if (tutorialManager != null && tutorialManager.directiveText != null)
            {
                tutorialManager.directiveText.transform.DOKill();
                tutorialManager.directiveText.transform.localScale = Vector3.one;
                tutorialManager.directiveText.transform.DOScale(Vector3.one * 1.2f, duration / 2f).SetEase(Ease.Linear).SetLoops(-1, LoopType.Yoyo);
            }
        }

        private void TutorialObjectsAnim()
        {
            GameObject[] highlightedObjects = referenceStep != null ? referenceStep.GetHighlightedObjects() : null;
            if (highlightedObjects == null || highlightedObjects.Length == 0 || highlightedObjects[0] == null)
                return;

            Transform startPointTransform = highlightedObjects[0].transform;

            // Detect the screen position of the start and end points to determine where the animation should be played
            Vector2 startAnchoredPos = GetAnchoredPosition(startPointTransform);

            tutorialObjectInstance.DOKill();
            tutorialObjectInstance.anchoredPosition = startAnchoredPos;

            ScaleAnimation();
        }

        private void ScaleAnimation()
        {
            tutorialObjectInstance.DOScale(Vector3.one * 1.1f, duration / 2f).SetEase(Ease.Linear).SetLoops(-1, LoopType.Yoyo);
        }
    }

    [System.Serializable]
    public class LookAndPointAtFirstHighlightedObject : TutorialAnimation
    {
        public float rotationOffset = -90f;

        public override void PlayAnim()
        {
            GameObject[] highlightedObjects = referenceStep != null ? referenceStep.GetHighlightedObjects() : null;
            if (tutorialObjectInstance == null || highlightedObjects == null || highlightedObjects.Length == 0)
                return;

            GameObject targetObject = highlightedObjects[0];
            if (targetObject == null)
                return;

            Transform targetTransform = targetObject.transform;
            Vector2 startAnchoredPos = GetAnchoredPosition(targetTransform);
            Vector2 targetAnchoredPos = ScreenToAnchoredPosition(GetScreenPosition(targetTransform));

            tutorialObjectInstance.DOKill();
            tutorialObjectInstance.anchoredPosition = startAnchoredPos;

            Vector2 direction = (targetAnchoredPos - startAnchoredPos).normalized;
            if (direction.sqrMagnitude > 0f)
            {
                float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg + rotationOffset;
                tutorialObjectInstance.rotation = Quaternion.Euler(0f, 0f, angle);
            }

            float halfDuration = Mathf.Max(0.05f, duration * 0.5f);

            if (isLoop)
            {
                tutorialObjectInstance.DOAnchorPos(targetAnchoredPos, halfDuration)
                    .SetEase(Ease.InOutSine)
                    .SetLoops(-1, LoopType.Yoyo);
            }
            else
            {
                tutorialObjectInstance.DOAnchorPos(targetAnchoredPos, halfDuration)
                    .SetEase(Ease.InOutSine)
                    .OnComplete(() =>
                    {
                        if (tutorialObjectInstance != null)
                        {
                            tutorialObjectInstance.DOAnchorPos(startAnchoredPos, halfDuration)
                                .SetEase(Ease.InOutSine);
                        }
                    });
            }
        }
    }

}

