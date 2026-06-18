using UnityEngine;
using Game.EventManagement;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using static Game.Grid3D;
using System;

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
            Rocket,
            Cannon,
            Torch,
            Hammer
        }

        private static bool HasBehavior(GridCell cell, ElementData.ElementBehaviorFlags flag)
        {
            return cell?.elementInfo?.elementData != null && cell.elementInfo.elementData.HasBehavior(flag);
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
                if (draggedCell?.elementInfo?.elementData != null &&
                    draggedCell.elementInfo.elementData is GarbageBagElementData)
                {
                    return;
                }

                if (GameManager.Instance != null && draggedCell?.elementInfo?.elementData != null &&
                    draggedCell.elementInfo.elementData)
                {
                    return;
                }

                if (HasBehavior(draggedCell, ElementData.ElementBehaviorFlags.NonSwappable))
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
            if ((fromCell?.elementInfo != null && fromCell.elementInfo.elementData.behaviorFlags.HasFlag(ElementData.ElementBehaviorFlags.NonSwappable)) ||
                (toCell?.elementInfo != null && toCell.elementInfo.powerUpType == ElementPowerUpType.Cauldron))
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

        public void BeginCannonPlacement()
        {
            pendingPlacementAction = PendingPlacementAction.Cannon;
            isPlacementReady = false;
        }

        public void BeginTorchPlacement()
        {
            pendingPlacementAction = PendingPlacementAction.Torch;
            isPlacementReady = false;
        }

        public void BeginHammerPlacement()
        {
            pendingPlacementAction = PendingPlacementAction.Hammer;
            isPlacementReady = false;
        }

        private static int WrapIndex(int value, int size)
        {
            if (size <= 0)
                return 0;

            int wrapped = value % size;
            if (wrapped < 0)
                wrapped += size;

            return wrapped;
        }

        private bool TryFindClosestPlayableCell(Vector2Int desired, out Vector2Int resolved)
        {
            resolved = desired;
            if (match3Grid == null)
                return false;

            int bestDistance = int.MaxValue;
            bool found = false;

            for (int x = 0; x < match3Grid.GridSize.x; x++)
            {
                for (int y = 0; y < match3Grid.GridSize.y; y++)
                {
                    Vector2Int candidate = new Vector2Int(x, y);
                    if (match3Grid.GetCellControllerAt(candidate) == null)
                        continue;

                    int dx = Mathf.Abs(candidate.x - desired.x);
                    int dy = Mathf.Abs(candidate.y - desired.y);
                    int distance = dx + dy;
                    int resolvedDx = Mathf.Abs(resolved.x - desired.x);
                    int resolvedDy = Mathf.Abs(resolved.y - desired.y);
                    if (!found ||
                        distance < bestDistance ||
                        (distance == bestDistance && dx < resolvedDx) ||
                        (distance == bestDistance && dx == resolvedDx && dy < resolvedDy))
                    {
                        found = true;
                        bestDistance = distance;
                        resolved = candidate;
                    }
                }
            }

            return found;
        }

        private bool TryFindClosestPlayableCellInColumn(int x, int desiredY, out Vector2Int resolved)
        {
            resolved = new Vector2Int(x, desiredY);
            if (match3Grid == null || x < 0 || x >= match3Grid.GridSize.x)
                return false;

            bool found = false;
            int bestDistance = int.MaxValue;

            for (int y = 0; y < match3Grid.GridSize.y; y++)
            {
                Vector2Int candidate = new Vector2Int(x, y);
                if (match3Grid.GetCellControllerAt(candidate) == null)
                    continue;

                int distance = Mathf.Abs(y - desiredY);
                if (!found || distance < bestDistance || (distance == bestDistance && y > resolved.y))
                {
                    found = true;
                    bestDistance = distance;
                    resolved = candidate;
                }
            }

            return found;
        }

        private bool TryFindClosestPlayableCellInRow(int desiredX, int y, out Vector2Int resolved)
        {
            resolved = new Vector2Int(desiredX, y);
            if (match3Grid == null || y < 0 || y >= match3Grid.GridSize.y)
                return false;

            bool found = false;
            int bestDistance = int.MaxValue;

            for (int x = 0; x < match3Grid.GridSize.x; x++)
            {
                Vector2Int candidate = new Vector2Int(x, y);
                if (match3Grid.GetCellControllerAt(candidate) == null)
                    continue;

                int distance = Mathf.Abs(x - desiredX);
                if (!found || distance < bestDistance || (distance == bestDistance && x > resolved.x))
                {
                    found = true;
                    bestDistance = distance;
                    resolved = candidate;
                }
            }

            return found;
        }

        private Vector2Int ResolveActionCenter(BoosterBarAction action, Vector2Int center)
        {
            if (match3Grid == null)
                return center;

            Vector2Int resolved = center;

            if (action != null)
            {
                if (action.overrideCellLocationX)
                    resolved.x = WrapIndex(action.fixedCellLocationX, match3Grid.GridSize.x);

                if (action.overrideCellLocationY)
                    resolved.y = WrapIndex(action.fixedCellLocationY, match3Grid.GridSize.y);
            }

            if (match3Grid.GetCellControllerAt(resolved) != null)
                return resolved;

            if (action != null)
            {
                if (action.overrideCellLocationY && !action.overrideCellLocationX &&
                    TryFindClosestPlayableCellInColumn(resolved.x, resolved.y, out Vector2Int sameColumn))
                {
                    return sameColumn;
                }

                if (action.overrideCellLocationX && !action.overrideCellLocationY &&
                    TryFindClosestPlayableCellInRow(resolved.x, resolved.y, out Vector2Int sameRow))
                {
                    return sameRow;
                }
            }

            if (TryFindClosestPlayableCell(resolved, out Vector2Int closestPlayable))
                return closestPlayable;

            return center;
        }

        private IEnumerator PlayPreExecutionAnimation(BoosterBarAction action, Vector2Int center)
        {
            if (action == null || action.preExecutionAnimator == null || string.IsNullOrEmpty(action.animationName) || match3Grid == null)
                yield break;

            center = ResolveActionCenter(action, center);
            if (match3Grid.GetCellControllerAt(center) == null)
                yield break;

            Vector3 cellPosition = match3Grid.GetCellPositionInGrid(center);
            Animator animator = Instantiate(action.preExecutionAnimator, cellPosition, Quaternion.identity);
            
            if (animator != null)
            {
                animator.Play(action.animationName);
                
                AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
                float animationLength = stateInfo.length;
                
                if (animationLength > 0)
                {
                    yield return new WaitForSeconds(animationLength);
                }
                else
                {
                    yield return new WaitForSeconds(1);
                }
            }
            
            Destroy(animator.gameObject);
        }

        public bool IsPlacementActionActive(string actionTypeName)
        {
            if (string.IsNullOrEmpty(actionTypeName))
                return false;

            switch (actionTypeName)
            {
                case "BombPlacementAction":
                    return pendingPlacementAction == PendingPlacementAction.Bomb;
                case "PlaceDiscoBallAction":
                    return pendingPlacementAction == PendingPlacementAction.DiscoBall;
                case "PlaceRocketAction":
                    return pendingPlacementAction == PendingPlacementAction.Rocket;
                case "CannonAction":
                    return pendingPlacementAction == PendingPlacementAction.Cannon;
                case "TorchAction":
                    return pendingPlacementAction == PendingPlacementAction.Torch;
                case "HammerAction":
                    return pendingPlacementAction == PendingPlacementAction.Hammer;
                default:
                    return false;
            }
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
            else if (actionToPlace == PendingPlacementAction.Cannon)
            {
                StartCoroutine(CannonPlacementRoutine(cell.Coordinates));
            }
            else if (actionToPlace == PendingPlacementAction.Torch)
            {
                StartCoroutine(TorchPlacementRoutine(cell.Coordinates));
            }
            else if (actionToPlace == PendingPlacementAction.Hammer)
            {
                StartCoroutine(HammerPlacementRoutine(cell.Coordinates));
            }
        }

        private IEnumerator TorchPlacementRoutine(Vector2Int coordinates)
        {
            ClearHintVisuals();
            isProcessing = true;

            TorchAction torchAction = ActionBarManager.Instance.actionBarItemList.Find(item => item is TorchAction) as TorchAction;
            if (torchAction == null)
            {
                isProcessing = false;
                yield break;
            }

            Vector2Int targetCenter = ResolveActionCenter(torchAction, coordinates);

            yield return StartCoroutine(PlayPreExecutionAnimation(torchAction, targetCenter));
            torchAction.currentCount--;
            EventManager.TriggerEvent(GameEvent.ACTION_SUCCESSFUL, new EventParam(paramStr: torchAction.ItemName));
            yield return StartCoroutine(match3Grid.ClearRowAt(targetCenter.y, false));
            yield return StartCoroutine(match3Grid.ResolveBoardAfterSpecialClear());

            isProcessing = false;
            idleTimer = 0f;
        }

        private IEnumerator BombPlacementRoutine(Vector2Int center)
        {
            ClearHintVisuals();
            isProcessing = true;
            BombPlacementAction bombAction = ActionBarManager.Instance.actionBarItemList.Find(item => item is BombPlacementAction) as BombPlacementAction;
            if (bombAction == null)
            {
                isProcessing = false;
                yield break;
            }

            Vector2Int targetCenter = ResolveActionCenter(bombAction, center);

            yield return StartCoroutine(PlayPreExecutionAnimation(bombAction, targetCenter));
            bombAction.currentCount--;
            yield return StartCoroutine(bombAction.BombThrowAnim(match3Grid.GetCellPositionInGrid(targetCenter)));
            EventManager.TriggerEvent(GameEvent.ACTION_SUCCESSFUL, new EventParam(paramStr: "Bomb Placement"));
            yield return StartCoroutine(match3Grid.ClearAreaAt(targetCenter, 1, false));
            yield return StartCoroutine(match3Grid.ResolveBoardAfterSpecialClear());
            isProcessing = false;
            idleTimer = 0f;
        }

        private IEnumerator HammerPlacementRoutine(Vector2Int center)
        {
            ClearHintVisuals();
            isProcessing = true;

            HammerAction hammerAction = ActionBarManager.Instance.actionBarItemList.Find(item => item is HammerAction) as HammerAction;
            if (hammerAction == null)
            {
                isProcessing = false;
                yield break;
            }

            Vector2Int targetCenter = ResolveActionCenter(hammerAction, center);

            yield return StartCoroutine(PlayPreExecutionAnimation(hammerAction, targetCenter));
            hammerAction.currentCount--;
            EventManager.TriggerEvent(GameEvent.ACTION_SUCCESSFUL, new EventParam(paramStr: hammerAction.ItemName));
            yield return StartCoroutine(match3Grid.ClearCellAt(targetCenter, false));
            yield return StartCoroutine(match3Grid.ResolveBoardAfterSpecialClear());

            isProcessing = false;
            idleTimer = 0f;
        }

        private IEnumerator CannonPlacementRoutine(Vector2Int center)
        {
            ClearHintVisuals();
            isProcessing = true;

            CannonAction cannonAction = ActionBarManager.Instance.actionBarItemList.Find(item => item is CannonAction) as CannonAction;
            if (cannonAction == null)
            {
                isProcessing = false;
                yield break;
            }

            Vector2Int targetCenter = ResolveActionCenter(cannonAction, center);

            yield return StartCoroutine(PlayPreExecutionAnimation(cannonAction, targetCenter));
            cannonAction.currentCount--;
            EventManager.TriggerEvent(GameEvent.ACTION_SUCCESSFUL, new EventParam(paramStr: cannonAction.ItemName));
            yield return StartCoroutine(match3Grid.ClearColumnAt(targetCenter.x, false));
            yield return StartCoroutine(match3Grid.ResolveBoardAfterSpecialClear());

            isProcessing = false;
            idleTimer = 0f;
        }

        private IEnumerator RocketPlacementRoutine(Vector2Int center)
        {
            ClearHintVisuals();
            isProcessing = true;

            PlaceRocketAction rocketAction = ActionBarManager.Instance.actionBarItemList.Find(item => item is PlaceRocketAction) as PlaceRocketAction;
            if (rocketAction == null)
            {
                isProcessing = false;
                yield break;
            }

            Vector2Int targetCenter = ResolveActionCenter(rocketAction, center);

            GridCell selectedCell = match3Grid.GetCellPublic(targetCenter);
            if (selectedCell == null || selectedCell.cellType != CellType.Normal || selectedCell.elementInfo == null ||
                selectedCell.elementInfo.powerUpType != ElementPowerUpType.None || selectedCell.elementInfo.elementData == null)
            {
                isProcessing = false;
                yield break;
            }

            yield return StartCoroutine(PlayPreExecutionAnimation(rocketAction, targetCenter));
            rocketAction.currentCount--;
            yield return StartCoroutine(rocketAction.RocketThrowAnim(match3Grid.GetCellPositionInGrid(targetCenter)));
            EventManager.TriggerEvent(GameEvent.ACTION_SUCCESSFUL, new EventParam(paramStr: rocketAction.ItemName));
            yield return StartCoroutine(match3Grid.PlaceHorizontalRocketActionAt(targetCenter));

            isProcessing = false;
            idleTimer = 0f;
        }

        private IEnumerator DiscoBallPlacementRoutine(Vector2Int center)
        {
            ClearHintVisuals();
            isProcessing = true;

            PlaceDiscoBallAction discoBallAction = ActionBarManager.Instance.actionBarItemList.Find(item => item is PlaceDiscoBallAction) as PlaceDiscoBallAction;
            if (discoBallAction == null)
            {
                isProcessing = false;
                yield break;
            }

            Vector2Int targetCenter = ResolveActionCenter(discoBallAction, center);

            GridCell selectedCell = match3Grid.GetCellPublic(targetCenter);
            if (selectedCell == null || selectedCell.cellType != CellType.Normal || selectedCell.elementInfo == null ||
                selectedCell.elementInfo.powerUpType != ElementPowerUpType.None || selectedCell.elementInfo.elementData == null)
            {
                isProcessing = false;
                yield break;
            }

            yield return StartCoroutine(PlayPreExecutionAnimation(discoBallAction, targetCenter));
            discoBallAction.currentCount--;
            yield return StartCoroutine(discoBallAction.DiscoBallThrowAnim(match3Grid.GetCellPositionInGrid(targetCenter)));
            EventManager.TriggerEvent(GameEvent.ACTION_SUCCESSFUL, new EventParam(paramStr: discoBallAction.ItemName));
            yield return StartCoroutine(match3Grid.PlaceDiscoBallActionAt(targetCenter));

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
            if (GameManager.Instance == null || TutorialManager.Instance == null)
                return true;

            return TutorialManager.Instance.IsElementInteractionAllowed(targetObject);
        }

        private bool ShouldBlockHintsForTutorial()
        {
            return GameManager.Instance != null
                && TutorialManager.Instance != null
                && TutorialManager.Instance.HasActiveStep;
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
