using UnityEngine;

namespace Game
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public class SafeAreaAdjuster : MonoBehaviour
    {
        [System.Flags]
        public enum SafeAreaDirection
        {
            None = 0,
            Left = 1 << 0,
            Right = 1 << 1,
            Top = 1 << 2,
            Bottom = 1 << 3,
            All = Left | Right | Top | Bottom
        }

        [SerializeField] protected SafeAreaDirection adjustDirections = SafeAreaDirection.Top;
        [SerializeField] protected Vector2 additionalPadding = Vector2.zero;
        [SerializeField] protected bool usePercentBasedPadding;
        [SerializeField] protected float minimumSafeAreaInsetPixels;
        [SerializeField] protected bool watchSafeAreaChanges = true;
        [SerializeField] protected bool ignoreInEditor = true;

        private RectTransform targetRect;
        private Vector2 initialAnchoredPosition;
        private bool hasCapturedInitialPosition;

        private Rect lastSafeArea;
        private Vector2Int lastScreenSize;
        private ScreenOrientation lastOrientation;

        protected virtual void Awake()
        {
            EnsureRectTransform();
            CaptureInitialPosition();
            ApplySafeArea(Screen.safeArea);
        }

        protected virtual void OnEnable()
        {
            EnsureRectTransform();
            CaptureInitialPosition();
            ApplySafeArea(Screen.safeArea);
        }

        protected virtual void OnValidate()
        {
            EnsureRectTransform();
            CaptureInitialPosition();
            ApplySafeArea(Screen.safeArea);
        }

        private void Update()
        {
            if (!watchSafeAreaChanges)
                return;

            if (HasSafeAreaStateChanged())
                ApplySafeArea(Screen.safeArea);
        }

        public void ApplySafeArea(Rect safeArea)
        {
            if (ignoreInEditor && Application.isEditor && !Application.isPlaying)
                return;

            EnsureRectTransform();
            if (targetRect == null)
                return;

            CaptureInitialPosition();
            float leftInset = safeArea.xMin;
            float rightInset = Screen.width - safeArea.xMax;
            float bottomInset = safeArea.yMin;
            float topInset = Screen.height - safeArea.yMax;

            float minInset = Mathf.Max(0f, minimumSafeAreaInsetPixels);
            if (leftInset < minInset) leftInset = 0f;
            if (rightInset < minInset) rightInset = 0f;
            if (bottomInset < minInset) bottomInset = 0f;
            if (topInset < minInset) topInset = 0f;

            Vector2 resolvedPadding = additionalPadding;
            if (usePercentBasedPadding)
            {
                resolvedPadding = new Vector2(
                    Screen.width * (additionalPadding.x * 0.01f),
                    Screen.height * (additionalPadding.y * 0.01f));
            }

            Vector2 offset = Vector2.zero;

            if ((adjustDirections & SafeAreaDirection.Left) != 0)
                offset.x += leftInset + resolvedPadding.x;
            if ((adjustDirections & SafeAreaDirection.Right) != 0)
                offset.x -= rightInset + resolvedPadding.x;
            if ((adjustDirections & SafeAreaDirection.Bottom) != 0)
                offset.y += bottomInset + resolvedPadding.y;
            if ((adjustDirections & SafeAreaDirection.Top) != 0)
                offset.y -= topInset + resolvedPadding.y;

            targetRect.anchoredPosition = initialAnchoredPosition + offset;

            lastSafeArea = safeArea;
            lastScreenSize = new Vector2Int(Screen.width, Screen.height);
            lastOrientation = Screen.orientation;
        }

        public void ResetToInitialPosition()
        {
            EnsureRectTransform();
            if (targetRect == null)
                return;

            CaptureInitialPosition();
            targetRect.anchoredPosition = initialAnchoredPosition;
        }

        protected void EnsureRectTransform()
        {
            if (targetRect == null)
                targetRect = GetComponent<RectTransform>();
        }

        protected void CaptureInitialPosition()
        {
            if (targetRect == null)
                return;

            if (hasCapturedInitialPosition)
                return;

            initialAnchoredPosition = targetRect.anchoredPosition;
            hasCapturedInitialPosition = true;
        }

        private bool HasSafeAreaStateChanged()
        {
            if (lastScreenSize.x != Screen.width || lastScreenSize.y != Screen.height)
                return true;

            if (lastOrientation != Screen.orientation)
                return true;

            return lastSafeArea != Screen.safeArea;
        }

#if UNITY_EDITOR
        [ContextMenu("Re-Capture Initial Position")]
        private void ReCaptureInitialPosition()
        {
            EnsureRectTransform();
            if (targetRect == null)
                return;

            initialAnchoredPosition = targetRect.anchoredPosition;
            hasCapturedInitialPosition = true;
            ApplySafeArea(Screen.safeArea);
        }
#endif
    }
}
