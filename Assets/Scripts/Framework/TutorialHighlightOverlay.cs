using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace Game
{
    /// <summary>
    /// Full-screen dark overlay that punches transparent spotlight cutouts over
    /// every <see cref="TutorialStep.HighlightedObjects"/> while a tutorial step is active.
    ///
    /// Scene Setup
    /// -----------
    ///  1. Create a Canvas (Screen Space – Overlay, high Sort Order, e.g. 999).
    ///  2. Add a child RawImage that stretches to fill the canvas (anchor: stretch/stretch).
    ///  3. Create a Material using the "UI/TutorialHighlight" shader and assign it
    ///     to the RawImage's Material slot.
    ///  4. Add this component to any GameObject in the scene and assign:
    ///       • overlayImage  ? the RawImage from step 2
    ///       • worldCamera   ? the camera that renders world-space objects
    ///         (leave null to fall back to Camera.main)
    ///  5. Assign this component to TutorialManager.highlightOverlay.
    /// </summary>
    [AddComponentMenu("Game/Tutorial/Tutorial Highlight Overlay")]
    public class TutorialHighlightOverlay : MonoBehaviour
    {
        // -----------------------------------------------------------------
        //  Shader property IDs (cached once)
        // -----------------------------------------------------------------
        private static readonly int ID_OverlayColor = Shader.PropertyToID("_OverlayColor");
        private static readonly int ID_RectCount    = Shader.PropertyToID("_RectCount");
        private static readonly int ID_CornerRadius = Shader.PropertyToID("_CornerRadius");
        private static readonly int ID_EdgeSoftness = Shader.PropertyToID("_EdgeSoftness");
        private static readonly int[] ID_Rects =
        {
            Shader.PropertyToID("_Rect0"),
            Shader.PropertyToID("_Rect1"),
            Shader.PropertyToID("_Rect2"),
            Shader.PropertyToID("_Rect3"),
        };
        private const int MaxSpotlights = 4;

        // -----------------------------------------------------------------
        //  Inspector
        // -----------------------------------------------------------------
        [Header("References")]
        [Tooltip("RawImage covering the full screen. Its material must use UI/TutorialHighlight.")]
        public RawImage overlayImage;

        [Tooltip("Camera used to project world-space GameObjects to screen space. Falls back to Camera.main.")]
        public Camera worldCamera;

        [Header("Appearance")]
        public Color overlayColor = new Color(0f, 0f, 0f, 0.75f);

        [Tooltip("Corner radius (screen pixels) for each spotlight cutout.")]
        public float cornerRadius = 16f;

        [Tooltip("Anti-alias softness (screen pixels) at the spotlight edge.")]
        public float edgeSoftness = 4f;

        [Tooltip("Extra padding (screen pixels) added around each highlighted object's bounds.")]
        public float padding = 10f;

        [Header("Transition")]
        public float fadeInDuration  = 0.25f;
        public float fadeOutDuration = 0.20f;

        [Tooltip("Seconds to wait before computing screen rects. Set this above the camera's initDelay (default 0.1 s) to ensure the camera has settled before the highlight is placed.")]
        public float highlightDelay = 0.15f;

        // -----------------------------------------------------------------
        //  Private state
        // -----------------------------------------------------------------
        private Material _mat;
        private bool     _shown;

        // -----------------------------------------------------------------
        //  Unity lifecycle
        // -----------------------------------------------------------------
        private void Awake()
        {
            if (overlayImage == null)
            {
                Debug.LogError("[TutorialHighlightOverlay] overlayImage is not assigned.", this);
                enabled = false;
                return;
            }

            // Overlay is visual-only: do not block gameplay/UI input raycasts.
            overlayImage.raycastTarget = false;

            // Instance material so we don't mutate the shared asset
            _mat = new Material(overlayImage.material != null
                ? overlayImage.material
                : new Material(Shader.Find("UI/TutorialHighlight")));

            overlayImage.material = _mat;

            // Start fully transparent
            _mat.SetColor(ID_OverlayColor, new Color(overlayColor.r, overlayColor.g, overlayColor.b, 0f));
            _mat.SetFloat(ID_RectCount, 0f);
            _mat.SetFloat(ID_CornerRadius, cornerRadius);
            _mat.SetFloat(ID_EdgeSoftness, edgeSoftness);

            gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            if (_mat != null) Destroy(_mat);
        }

        // -----------------------------------------------------------------
        //  Public API – called by TutorialManager
        // -----------------------------------------------------------------

        /// <summary>
        /// Shows the overlay with spotlight cutouts over every object in
        /// <paramref name="targets"/>.  Safe to call again to update targets.
        /// </summary>
        public void Show(IList<GameObject> targets)
        {
            if (_mat == null) return;

            gameObject.SetActive(true);

            DOTween.Kill(this);
            DOTween.To(
                ()  => _mat.GetColor(ID_OverlayColor),
                col => _mat.SetColor(ID_OverlayColor, col),
                new Color(overlayColor.r, overlayColor.g, overlayColor.b, overlayColor.a),
                fadeInDuration
            ).SetId(this);

            _shown = true;

            // Delay rect computation until the camera has finished adjusting.
            if (_showRectsCoroutine != null) StopCoroutine(_showRectsCoroutine);
            _showRectsCoroutine = StartCoroutine(SetRectsAfterDelay(targets));
        }

        private Coroutine _showRectsCoroutine;

        private IEnumerator SetRectsAfterDelay(IList<GameObject> targets)
        {
            if (highlightDelay > 0f)
                yield return new WaitForSeconds(highlightDelay);

            SetRects(targets);
            EventManager.TriggerEvent(GameEvent.HIGHLIGHT_UPDATED);
            _showRectsCoroutine = null;
        }

        /// <summary>Fades the overlay out and deactivates the GameObject.</summary>
        public void Hide()
        {
            if (!_shown || _mat == null) return;
            _shown = false;

            DOTween.Kill(this);
            DOTween.To(
                ()  => _mat.GetColor(ID_OverlayColor),
                col => _mat.SetColor(ID_OverlayColor, col),
                new Color(overlayColor.r, overlayColor.g, overlayColor.b, 0f),
                fadeOutDuration
            ).SetId(this).OnComplete(() => gameObject.SetActive(false));
        }
        // -----------------------------------------------------------------
        //  Rect calculation
        // -----------------------------------------------------------------
        private void SetRects(IList<GameObject> targets)
        {
            Camera cam   = worldCamera != null ? worldCamera : Camera.main;
            int    count = Mathf.Min(targets != null ? targets.Count : 0, MaxSpotlights);

            _mat.SetFloat(ID_RectCount,    count);
            _mat.SetFloat(ID_CornerRadius, cornerRadius);
            _mat.SetFloat(ID_EdgeSoftness, edgeSoftness);

            Debug.Log($"[TutorialHighlightOverlay] SetRects — target count: {count} | screen: {Screen.width}x{Screen.height} | camera: {(cam != null ? cam.name : "NULL")}");

            for (int i = 0; i < MaxSpotlights; i++)
            {
                if (i < count && targets[i] != null)
                {
                    Rect sr = ScreenRectFor(targets[i], cam);

                    Vector4 rectVec = new Vector4(
                        sr.xMin - padding,
                        sr.yMin - padding,
                        sr.xMax + padding,
                        sr.yMax + padding);

                    _mat.SetVector(ID_Rects[i], rectVec);

                    Debug.Log(
                        $"[TutorialHighlightOverlay] Slot {i} | GameObject: \"{targets[i].name}\" | " +
                        $"raw screen rect: xMin={sr.xMin:F1} yMin={sr.yMin:F1} xMax={sr.xMax:F1} yMax={sr.yMax:F1} | " +
                        $"padded shader rect: ({rectVec.x:F1}, {rectVec.y:F1}, {rectVec.z:F1}, {rectVec.w:F1})",
                        targets[i]);
                }
                else
                {
                    _mat.SetVector(ID_Rects[i], Vector4.zero);
                }
            }

        }

        // -----------------------------------------------------------------
        //  Screen-rect helpers
        // -----------------------------------------------------------------

        private static Rect ScreenRectFor(GameObject go, Camera cam)
        {
            RectTransform rt = go.GetComponent<RectTransform>();
            if (rt != null)
            {
                Canvas canvas = rt.GetComponentInParent<Canvas>();
                if (canvas != null && canvas.renderMode == RenderMode.ScreenSpaceOverlay)
                {
                    Vector3[] corners = new Vector3[4];
                    rt.GetWorldCorners(corners);
                    Rect result = CornersToRect(corners);
                    Debug.Log(
                        $"[TutorialHighlightOverlay] \"{go.name}\" ? RectTransform (SS-Overlay) | " +
                        $"corners BL={corners[0]} TL={corners[1]} TR={corners[2]} BR={corners[3]} | " +
                        $"rect: xMin={result.xMin:F1} yMin={result.yMin:F1} xMax={result.xMax:F1} yMax={result.yMax:F1}",
                        go);
                    return result;
                }

                if (cam != null)
                {
                    Vector3[] corners = new Vector3[4];
                    rt.GetWorldCorners(corners);
                    Rect result = ProjectedCornersToRect(corners, cam);
                    Debug.Log(
                        $"[TutorialHighlightOverlay] \"{go.name}\" ? RectTransform (Camera/World canvas) | " +
                        $"world corners BL={corners[0]} TL={corners[1]} TR={corners[2]} BR={corners[3]} | " +
                        $"projected rect: xMin={result.xMin:F1} yMin={result.yMin:F1} xMax={result.xMax:F1} yMax={result.yMax:F1}",
                        go);
                    return result;
                }
            }

            if (cam != null)
            {
                Bounds b = WorldBoundsOf(go);
                if (b.size != Vector3.zero)
                {
                    Rect result = BoundsToScreenRect(b, cam);
                    Debug.Log(
                        $"[TutorialHighlightOverlay] \"{go.name}\" ? world-space bounds | " +
                        $"bounds center={b.center} size={b.size} | " +
                        $"screen rect: xMin={result.xMin:F1} yMin={result.yMin:F1} xMax={result.xMax:F1} yMax={result.yMax:F1}",
                        go);
                    return result;
                }

                Vector3 worldPos  = go.transform.position;
                Vector3 screenPos = cam.WorldToScreenPoint(worldPos);
                Rect fallback = new Rect(screenPos.x - 32f, screenPos.y - 32f, 64f, 64f);
                Debug.Log(
                    $"[TutorialHighlightOverlay] \"{go.name}\" ? fallback single point | " +
                    $"world pos={worldPos} ? screen pos={screenPos} | " +
                    $"rect: xMin={fallback.xMin:F1} yMin={fallback.yMin:F1} xMax={fallback.xMax:F1} yMax={fallback.yMax:F1}",
                    go);
                return fallback;
            }

            Debug.LogWarning($"[TutorialHighlightOverlay] \"{go.name}\" ? no camera available, returning Rect.zero.", go);
            return Rect.zero;
        }

        private static Rect CornersToRect(Vector3[] corners)
        {
            float xMin = float.MaxValue, yMin = float.MaxValue;
            float xMax = float.MinValue, yMax = float.MinValue;
            for (int i = 0; i < corners.Length; i++)
            {
                xMin = Mathf.Min(xMin, corners[i].x);
                yMin = Mathf.Min(yMin, corners[i].y);
                xMax = Mathf.Max(xMax, corners[i].x);
                yMax = Mathf.Max(yMax, corners[i].y);
            }
            return Rect.MinMaxRect(xMin, yMin, xMax, yMax);
        }

        private static Rect ProjectedCornersToRect(Vector3[] worldCorners, Camera cam)
        {
            float xMin = float.MaxValue, yMin = float.MaxValue;
            float xMax = float.MinValue, yMax = float.MinValue;
            for (int i = 0; i < worldCorners.Length; i++)
            {
                Vector3 sp = cam.WorldToScreenPoint(worldCorners[i]);
                xMin = Mathf.Min(xMin, sp.x);
                yMin = Mathf.Min(yMin, sp.y);
                xMax = Mathf.Max(xMax, sp.x);
                yMax = Mathf.Max(yMax, sp.y);
            }
            return Rect.MinMaxRect(xMin, yMin, xMax, yMax);
        }

        private static Rect BoundsToScreenRect(Bounds b, Camera cam)
        {
            // Project all 8 corners of the AABB
            Vector3 min = b.min, max = b.max;
            Vector3[] pts =
            {
                min,
                max,
                new Vector3(min.x, min.y, max.z),
                new Vector3(min.x, max.y, min.z),
                new Vector3(min.x, max.y, max.z),
                new Vector3(max.x, min.y, min.z),
                new Vector3(max.x, min.y, max.z),
                new Vector3(max.x, max.y, min.z),
            };
            float xMin = float.MaxValue, yMin = float.MaxValue;
            float xMax = float.MinValue, yMax = float.MinValue;
            for (int i = 0; i < pts.Length; i++)
            {
                Vector3 sp = cam.WorldToScreenPoint(pts[i]);
                xMin = Mathf.Min(xMin, sp.x);
                yMin = Mathf.Min(yMin, sp.y);
                xMax = Mathf.Max(xMax, sp.x);
                yMax = Mathf.Max(yMax, sp.y);
            }
            return Rect.MinMaxRect(xMin, yMin, xMax, yMax);
        }

        private static Bounds WorldBoundsOf(GameObject go)
        {
            Renderer[] rends = go.GetComponentsInChildren<Renderer>(true);
            if (rends.Length > 0)
            {
                Bounds b = rends[0].bounds;
                for (int i = 1; i < rends.Length; i++) b.Encapsulate(rends[i].bounds);
                return b;
            }
            Collider[] cols = go.GetComponentsInChildren<Collider>(true);
            if (cols.Length > 0)
            {
                Bounds b = cols[0].bounds;
                for (int i = 1; i < cols.Length; i++) b.Encapsulate(cols[i].bounds);
                return b;
            }
            return new Bounds(go.transform.position, Vector3.zero);
        }
    }
}
