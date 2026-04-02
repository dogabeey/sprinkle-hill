using UnityEngine;
using System.Collections;
using static Game.Grid3D;

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
        private bool isBombPlacementPending;
        private bool isBombPlacementReady;

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

            if (isBombPlacementPending)
            {
                if (Input.GetMouseButtonDown(0))
                {
                    EventManager.TriggerEvent(GameEvent.INPUT_RECEIVED);
                    isBombPlacementReady = true;
                }
                else if (isBombPlacementReady && Input.GetMouseButtonUp(0))
                {
                    TryPlaceBomb();
                }
                return;
            }

            if (Input.GetMouseButtonDown(0))
            {
                EventManager.TriggerEvent(GameEvent.INPUT_RECEIVED);
                TryBeginDrag();
            }
            else if (Input.GetMouseButton(0) && draggedElement != null && !dragConsumed)
            {
                TryCommitDrag();
            }
            else if (Input.GetMouseButtonUp(0))
            {
                TryHandleClickActivationOrCancel();
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

        private void TryHandleClickActivationOrCancel()
        {
            if (draggedElement != null && !dragConsumed && match3Grid != null)
            {
                if (match3Grid.TryGetElementPosition(draggedElement, out Vector2Int pos))
                {
                    GridCell cell = match3Grid.GetCellPublic(pos);
                    if (cell?.elementInfo != null && PowerUpHandler.IsSpecialPowerUp(cell.elementInfo.powerUpType))
                    {
                        draggedElement.SetSelected(false);
                        draggedElement = null;
                        dragConsumed = false;
                        StartCoroutine(ActivatePowerUpRoutine(pos));
                        return;
                    }
                }
            }

            CancelDrag();
        }

        private IEnumerator ActivatePowerUpRoutine(Vector2Int pos)
        {
            isProcessing = true;
            yield return StartCoroutine(match3Grid.SwapAndMatch(pos, pos));
            isProcessing = false;
        }

        private IEnumerator SwapAndMatchRoutine(Vector2Int firstPos, Vector2Int secondPos)
        {
            isProcessing = true;

            yield return StartCoroutine(match3Grid.SwapAndMatch(firstPos, secondPos));

            isProcessing = false;
        }

        /// <summary>
        /// Puts the input controller into bomb placement mode.
        /// The next cell the player taps will be the bomb's target.
        /// </summary>
        public void BeginBombPlacement()
        {
            isBombPlacementPending = true;
            isBombPlacementReady = false;
        }

        private void TryPlaceBomb()
        {
            Camera cam = inputCamera != null ? inputCamera : Camera.main;
            if (cam == null || match3Grid == null)
            {
                isBombPlacementPending = false;
                Debug.LogWarning("Cannot place bomb: No input camera or grid reference.");
                return;
            }

            GridCellController cell = GetCellAtScreenPos(cam, Input.mousePosition);
            if (cell == null)
            {
                isBombPlacementPending = false;
                Debug.LogWarning("Cannot place bomb: No cell found at the screen position.");
                return;
            }

            isBombPlacementPending = false;
            ActionBarManager actionBarManager = GameManager.Instance.actionBarManager;
            StartCoroutine(BombPlacementRoutine(cell.Coordinates));
        }

        private IEnumerator BombPlacementRoutine(Vector2Int center)
        {
            isProcessing = true;
            BombPlacementAction bombAction = GameManager.Instance.actionBarManager.actionBarItemList.Find(item => item is BombPlacementAction) as BombPlacementAction;
            bombAction.CurrentCount--;
            yield return StartCoroutine(bombAction.BombThrowAnim(match3Grid.GetCellPositionInGrid(center)));
            EventManager.TriggerEvent(GameEvent.ACTION_SUCCESSFUL, new EventParam(paramStr: "Bomb Placement"));
            yield return StartCoroutine(match3Grid.ClearAreaAt(center, 1));
            yield return StartCoroutine(match3Grid.ApplyGravityPublic());
            isProcessing = false;
        }

        private GridCellController GetCellAtScreenPos(Camera cam, Vector3 screenPos)
        {
            Ray ray = cam.ScreenPointToRay(screenPos);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                GridCellController cell = hit.collider.GetComponentInParent<GridCellController>();
                if (cell != null) return cell;
            }

            RaycastHit2D hit2D = Physics2D.GetRayIntersection(ray);
            if (hit2D.collider != null)
            {
                GridCellController cell = hit2D.collider.GetComponentInParent<GridCellController>();
                if (cell != null) return cell;
            }

            return null;
        }
    }
}