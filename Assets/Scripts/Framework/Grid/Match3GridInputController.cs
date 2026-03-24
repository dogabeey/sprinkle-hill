using UnityEngine;
using System.Collections;

namespace Game
{
    public class Match3GridInputController : MonoBehaviour
    {
        [SerializeField] private Match3Grid match3Grid;
        [SerializeField] private Camera inputCamera;
        [SerializeField] private float minDragDistance = 20f; // pixels

        private GridElement_Match3Game draggedElement;
        private Vector2 dragStartScreenPos;
        private bool isProcessing;
        private bool dragConsumed;

        private void Awake()
        {
            if (inputCamera == null)
            {
                inputCamera = Camera.main;
            }
        }

        private void Update()
        {
            if (isProcessing)
            {
                return;
            }

            if (Input.GetMouseButtonDown(0))
            {
                TryBeginDrag();
            }
            else if (Input.GetMouseButton(0) && draggedElement != null && !dragConsumed)
            {
                TryCommitDrag();
            }
            else if (Input.GetMouseButtonUp(0))
            {
                CancelDrag();
            }
        }

        private void TryBeginDrag()
        {
            if (match3Grid == null)
            {
                return;
            }

            Camera cam = inputCamera != null ? inputCamera : Camera.main;
            if (cam == null)
            {
                return;
            }

            GridElement_Match3Game element = GetElementAtScreenPos(cam, Input.mousePosition);
            if (element == null || element.ownerGrid != match3Grid)
            {
                return;
            }

            draggedElement = element;
            dragStartScreenPos = Input.mousePosition;
            dragConsumed = false;
            draggedElement.SetSelected(true);
        }

        private void TryCommitDrag()
        {
            Vector2 dragDelta = (Vector2)Input.mousePosition - dragStartScreenPos;

            if (dragDelta.magnitude < minDragDistance)
            {
                return;
            }

            if (!match3Grid.TryGetElementPosition(draggedElement, out Vector2Int fromPos))
            {
                CancelDrag();
                return;
            }

            Vector2Int direction = GetDominantDirection(dragDelta);
            Vector2Int toPos = fromPos + direction;

            if (!match3Grid.IsValidPosition(toPos))
            {
                CancelDrag();
                return;
            }

            GridElement_Match3Game source = draggedElement;
            dragConsumed = true;
            source.SetSelected(false);
            draggedElement = null;

            StartCoroutine(SwapAndMatchRoutine(fromPos, toPos));
        }

        private void CancelDrag()
        {
            if (draggedElement != null)
            {
                draggedElement.SetSelected(false);
                draggedElement = null;
            }
            dragConsumed = false;
        }

        // X maps directly: screen right  ? grid X+1
        // Y is inverted:   screen up     ? world Y+  ? grid Y-1  (TopLeft layout: -j * spacing.y)
        private static Vector2Int GetDominantDirection(Vector2 delta)
        {
            if (Mathf.Abs(delta.x) >= Mathf.Abs(delta.y))
            {
                return delta.x > 0 ? Vector2Int.right : Vector2Int.left;
            }
            else
            {
                return delta.y > 0 ? Vector2Int.down : Vector2Int.up;
            }
        }

        private GridElement_Match3Game GetElementAtScreenPos(Camera cam, Vector3 screenPos)
        {
            Ray ray = cam.ScreenPointToRay(screenPos);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                return hit.collider.GetComponentInParent<GridElement_Match3Game>();
            }

            RaycastHit2D hit2D = Physics2D.GetRayIntersection(ray);
            if (hit2D.collider != null)
            {
                return hit2D.collider.GetComponentInParent<GridElement_Match3Game>();
            }

            return null;
        }

        private IEnumerator SwapAndMatchRoutine(Vector2Int firstPos, Vector2Int secondPos)
        {
            isProcessing = true;

            yield return StartCoroutine(match3Grid.SwapAndMatch(firstPos, secondPos));

            isProcessing = false;
        }
    }
}