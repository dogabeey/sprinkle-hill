using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using static Game.Grid3D;

namespace Game
{
    public class Match3GridInputController : MonoBehaviour
    {
        [SerializeField] private Match3Grid match3Grid;
        [SerializeField] private Camera inputCamera;
        [SerializeField] private float minDragDistance = 20f; // pixels
        [SerializeField] private float idleHintDelay = 5f;
        [SerializeField] private float hintMoveDuration = 1.4f;
        [SerializeField] private float hintMoveAmount = 0.1f;

        private GridElement_Match3Game draggedElement;
        private Vector2 dragStartScreenPos;
        private bool isProcessing;
        private bool dragConsumed;
        private PendingPlacementAction pendingPlacementAction;
        private bool isPlacementReady;
        private float idleTimer;
        private bool hintActive;
        private Tween hintTweenA;
        private Tween hintTweenB;
        private GridElement hintedElementA;
        private GridElement hintedElementB;
        private Vector3 hintedElementAStartPos;
        private Vector3 hintedElementBStartPos;
        private readonly List<GridCellController> outlinedHintCells = new List<GridCellController>();

        private enum PendingPlacementAction
        {
            None,
            Bomb,
            DiscoBall,
            Rocket
        }

        private void Awake()
        {
            if (inputCamera == null)
            {
                inputCamera = Camera.main;
            }
        }

        private void OnEnable()
        {
            EventManager.StartListening(GameEvent.INPUT_RECEIVED, OnInputReceived);
            EventManager.StartListening(GameEvent.GRID_STABLE, OnGridStable);
            EventManager.StartListening(GameEvent.ELEMENTS_SWAPPED, OnBoardChanged);
        }

        private void OnDisable()
        {
            EventManager.StopListening(GameEvent.INPUT_RECEIVED, OnInputReceived);
            EventManager.StopListening(GameEvent.GRID_STABLE, OnGridStable);
            EventManager.StopListening(GameEvent.ELEMENTS_SWAPPED, OnBoardChanged);
            ClearHintVisuals();
        }

        private void Update()
        {
            if (isProcessing)
            {
                return;
            }

            if (ShouldBlockBoardInput())
            {
                CancelDrag();
                return;
            }

            if (ShouldBlockHintsForTutorial())
            {
                idleTimer = 0f;
                if (hintActive)
                {
                    ClearHintVisuals();
                }
            }

            if (pendingPlacementAction != PendingPlacementAction.None)
            {
                if (Input.GetMouseButtonDown(0))
                {
                    EventManager.TriggerEvent(GameEvent.INPUT_RECEIVED);
                    isPlacementReady = true;
                }
                else if (isPlacementReady && Input.GetMouseButtonUp(0))
                {
                    TryPlacePendingAction();
                }
                return;
            }

            if (draggedElement == null)
            {
                if (!ShouldBlockHintsForTutorial())
                {
                    idleTimer += Time.deltaTime;
                    if (!hintActive && idleTimer >= idleHintDelay)
                    {
                        ShowIdleHint();
                    }
                }
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

            if (!IsTutorialInputAllowed(element.gameObject))
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
            if (draggedElement != null && match3Grid != null && match3Grid.TryGetElementPosition(draggedElement, out Vector2Int draggedPos))
            {
                GridCell draggedCell = match3Grid.GetCellPublic(draggedPos);
                if (draggedCell?.elementInfo != null && draggedCell.elementInfo.powerUpType == ElementPowerUpType.Cauldron)
                {
                    return;
                }
                if (GameManager.Instance != null && GameManager.Instance.CurrentLevel is LevelScene_Match3Game levelScene &&
                    draggedCell?.elementInfo?.elementData != null && levelScene.garbageBagElementData == draggedCell.elementInfo.elementData)
                {
                    return;
                }
            }

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

            Camera cam = inputCamera != null ? inputCamera : Camera.main;
            if (cam == null)
            {
                CancelDrag();
                return;
            }

            GridElement_Match3Game hoveredElement = GetElementAtScreenPos(cam, Input.mousePosition);
            if (hoveredElement == null || hoveredElement == draggedElement || hoveredElement.ownerGrid != match3Grid)
            {
                return;
            }

            if (!IsTutorialInputAllowed(hoveredElement.gameObject))
            {
                CancelDrag();
                return;
            }

            if (!match3Grid.TryGetElementPosition(hoveredElement, out Vector2Int toPos))
            {
                CancelDrag();
                return;
            }

            if (!Match3Grid.AreAdjacent(fromPos, toPos))
            {
                return;
            }

            GridCell fromCell = match3Grid.GetCellPublic(fromPos);
            GridCell toCell = match3Grid.GetCellPublic(toPos);
            if ((fromCell?.elementInfo != null && fromCell.elementInfo.powerUpType == ElementPowerUpType.Cauldron) ||
                (toCell?.elementInfo != null && toCell.elementInfo.powerUpType == ElementPowerUpType.Cauldron))
            {
                CancelDrag();
                return;
            }

            LevelScene_Match3Game currentLevel = GameManager.Instance != null ? GameManager.Instance.CurrentLevel as LevelScene_Match3Game : null;
            if (currentLevel != null &&
                ((fromCell?.elementInfo?.elementData != null && currentLevel.garbageBagElementData == fromCell.elementInfo.elementData) ||
                 (toCell?.elementInfo?.elementData != null && currentLevel.garbageBagElementData == toCell.elementInfo.elementData)))
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
                        if (cell.elementInfo.powerUpType == ElementPowerUpType.Cauldron && !match3Grid.IsCauldronReadyAt(pos))
                        {
                            draggedElement.SetSelected(false);
                            draggedElement = null;
                            dragConsumed = false;
                            return;
                        }

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
            ClearHintVisuals();
            isProcessing = true;
            yield return StartCoroutine(match3Grid.SwapAndMatch(pos, pos));
            isProcessing = false;
            idleTimer = 0f;
        }

        private IEnumerator SwapAndMatchRoutine(Vector2Int firstPos, Vector2Int secondPos)
        {
            ClearHintVisuals();
            isProcessing = true;

            yield return StartCoroutine(match3Grid.SwapAndMatch(firstPos, secondPos));

            isProcessing = false;
            idleTimer = 0f;
        }

        /// <summary>
        /// Puts the input controller into bomb placement mode.
        /// The next cell the player taps will be the bomb's target.
        /// </summary>
        public void BeginBombPlacement()
        {
            pendingPlacementAction = PendingPlacementAction.Bomb;
            isPlacementReady = false;
        }

        public void BeginDiscoBallPlacement()
        {
            pendingPlacementAction = PendingPlacementAction.DiscoBall;
            isPlacementReady = false;
        }

        public void BeginRocketPlacement()
        {
            pendingPlacementAction = PendingPlacementAction.Rocket;
            isPlacementReady = false;
        }

        private void TryPlacePendingAction()
        {
            Camera cam = inputCamera != null ? inputCamera : Camera.main;
            if (cam == null || match3Grid == null)
            {
                pendingPlacementAction = PendingPlacementAction.None;
                Debug.LogWarning("Cannot place action: No input camera or grid reference.");
                return;
            }

            GridCellController cell = GetCellAtScreenPos(cam, Input.mousePosition);
            if (cell == null)
            {
                pendingPlacementAction = PendingPlacementAction.None;
                Debug.LogWarning("Cannot place action: No cell found at the screen position.");
                return;
            }

            GridElement selectedElement = match3Grid.GetElementAt(cell.Coordinates);
            if (selectedElement == null || !IsTutorialInputAllowed(selectedElement.gameObject))
            {
                pendingPlacementAction = PendingPlacementAction.None;
                return;
            }

            PendingPlacementAction actionToPlace = pendingPlacementAction;
            pendingPlacementAction = PendingPlacementAction.None;

            if (actionToPlace == PendingPlacementAction.Bomb)
            {
                StartCoroutine(BombPlacementRoutine(cell.Coordinates));
            }
            else if (actionToPlace == PendingPlacementAction.DiscoBall)
            {
                StartCoroutine(DiscoBallPlacementRoutine(cell.Coordinates));
            }
            else if (actionToPlace == PendingPlacementAction.Rocket)
            {
                StartCoroutine(RocketPlacementRoutine(cell.Coordinates));
            }
        }

        private IEnumerator BombPlacementRoutine(Vector2Int center)
        {
            ClearHintVisuals();
            isProcessing = true;
            BombPlacementAction bombAction = GameManager.Instance.actionBarManager.actionBarItemList.Find(item => item is BombPlacementAction) as BombPlacementAction;
            if (bombAction == null)
            {
                isProcessing = false;
                yield break;
            }

            bombAction.CurrentCount--;
            yield return StartCoroutine(bombAction.BombThrowAnim(match3Grid.GetCellPositionInGrid(center)));
            EventManager.TriggerEvent(GameEvent.ACTION_SUCCESSFUL, new EventParam(paramStr: "Bomb Placement"));
            yield return StartCoroutine(match3Grid.ClearAreaAt(center, 1));
            yield return StartCoroutine(match3Grid.ApplyGravityPublic());
            isProcessing = false;
            idleTimer = 0f;
        }

        private IEnumerator RocketPlacementRoutine(Vector2Int center)
        {
            ClearHintVisuals();
            isProcessing = true;

            PlaceRocketAction rocketAction = GameManager.Instance.actionBarManager.actionBarItemList.Find(item => item is PlaceRocketAction) as PlaceRocketAction;
            if (rocketAction == null)
            {
                isProcessing = false;
                yield break;
            }

            GridCell selectedCell = match3Grid.GetCellPublic(center);
            if (selectedCell == null || selectedCell.cellType != CellType.Normal || selectedCell.elementInfo == null ||
                selectedCell.elementInfo.powerUpType != ElementPowerUpType.None || selectedCell.elementInfo.elementData == null)
            {
                isProcessing = false;
                yield break;
            }

            rocketAction.CurrentCount--;
            yield return StartCoroutine(rocketAction.RocketThrowAnim(match3Grid.GetCellPositionInGrid(center)));
            EventManager.TriggerEvent(GameEvent.ACTION_SUCCESSFUL, new EventParam(paramStr: rocketAction.ActionName));
            yield return StartCoroutine(match3Grid.PlaceHorizontalRocketActionAt(center));

            isProcessing = false;
            idleTimer = 0f;
        }

        private IEnumerator DiscoBallPlacementRoutine(Vector2Int center)
        {
            ClearHintVisuals();
            isProcessing = true;

            PlaceDiscoBallAction discoBallAction = GameManager.Instance.actionBarManager.actionBarItemList.Find(item => item is PlaceDiscoBallAction) as PlaceDiscoBallAction;
            if (discoBallAction == null)
            {
                isProcessing = false;
                yield break;
            }

            GridCell selectedCell = match3Grid.GetCellPublic(center);
            if (selectedCell == null || selectedCell.cellType != CellType.Normal || selectedCell.elementInfo == null ||
                selectedCell.elementInfo.powerUpType != ElementPowerUpType.None || selectedCell.elementInfo.elementData == null)
            {
                isProcessing = false;
                yield break;
            }

            discoBallAction.CurrentCount--;
            yield return StartCoroutine(discoBallAction.DiscoBallThrowAnim(match3Grid.GetCellPositionInGrid(center)));
            EventManager.TriggerEvent(GameEvent.ACTION_SUCCESSFUL, new EventParam(paramStr: discoBallAction.ActionName));
            yield return StartCoroutine(match3Grid.PlaceDiscoBallActionAt(center));

            isProcessing = false;
            idleTimer = 0f;
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

        private bool IsTutorialInputAllowed(GameObject targetObject)
        {
            if (GameManager.Instance == null || GameManager.Instance.tutorialManager == null)
                return true;

            return GameManager.Instance.tutorialManager.IsElementInteractionAllowed(targetObject);
        }

        private bool ShouldBlockHintsForTutorial()
        {
            return GameManager.Instance != null
                && GameManager.Instance.tutorialManager != null
                && GameManager.Instance.tutorialManager.HasActiveStep;
        }

        private bool ShouldBlockBoardInput()
        {
            LevelScene currentLevel = GameManager.Instance != null ? GameManager.Instance.CurrentLevel : null;
            return currentLevel != null && (currentLevel.isPaused || currentLevel.isEnded);
        }

        private void OnInputReceived(EventParam _)
        {
            idleTimer = 0f;
            ClearHintVisuals();
        }

        private void OnGridStable(EventParam _)
        {
            idleTimer = 0f;
            ClearHintVisuals();
        }

        private void OnBoardChanged(EventParam _)
        {
            idleTimer = 0f;
            ClearHintVisuals();
        }

        private void ShowIdleHint()
        {
            if (match3Grid == null) return;
            if (!match3Grid.TryGetRandomPossibleMove(out Vector2Int mover, out Vector2Int target, out List<Vector2Int> matchedGroup))
                return;

            GridElement elementA = match3Grid.GetElementAt(mover);
            GridElement elementB = match3Grid.GetElementAt(target);
            if (elementA == null || elementB == null)
                return;

            hintedElementA = elementA;
            hintedElementB = elementB;

            Vector3 startA = elementA.transform.position;
            Vector3 startB = elementB.transform.position;
            hintedElementAStartPos = startA;
            hintedElementBStartPos = startB;
            Vector3 toA = Vector3.Lerp(startA, startB, Mathf.Clamp01(hintMoveAmount));
            Vector3 toB = Vector3.Lerp(startB, startA, Mathf.Clamp01(hintMoveAmount));

            hintTweenA = elementA.transform.DOMove(toA, hintMoveDuration).SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo);
            hintTweenB = elementB.transform.DOMove(toB, hintMoveDuration).SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo);

            DrawMatchedGroupOutline(matchedGroup);
            hintActive = true;
        }

        private void DrawMatchedGroupOutline(List<Vector2Int> group)
        {
            ClearHintOutline();
            if (group == null || group.Count == 0) return;

            HashSet<Vector2Int> positions = new HashSet<Vector2Int>(group);
            foreach (Vector2Int pos in positions)
            {
                GridCellController cell = match3Grid.GetCellControllerAt(pos);
                if (cell == null) continue;

                bool up = !positions.Contains(pos + Vector2Int.up);
                bool down = !positions.Contains(pos + Vector2Int.down);
                bool left = !positions.Contains(pos + Vector2Int.left);
                bool right = !positions.Contains(pos + Vector2Int.right);

                cell.SetBorders(up, down, left, right);
                outlinedHintCells.Add(cell);
            }
        }

        private void ClearHintOutline()
        {
            for (int i = 0; i < outlinedHintCells.Count; i++)
            {
                if (outlinedHintCells[i] != null)
                    outlinedHintCells[i].ClearBorders();
            }
            outlinedHintCells.Clear();
        }

        private void ClearHintVisuals()
        {
            hintActive = false;

            if (hintTweenA != null && hintTweenA.IsActive()) hintTweenA.Kill();
            if (hintTweenB != null && hintTweenB.IsActive()) hintTweenB.Kill();
            hintTweenA = null;
            hintTweenB = null;

            if (hintedElementA != null) hintedElementA.transform.position = hintedElementAStartPos;
            if (hintedElementB != null) hintedElementB.transform.position = hintedElementBStartPos;
            hintedElementA = null;
            hintedElementB = null;

            ClearHintOutline();
        }
    }
}