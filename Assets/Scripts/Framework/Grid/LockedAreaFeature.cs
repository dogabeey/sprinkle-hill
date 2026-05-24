using Sirenix.OdinInspector;
using System.Collections.Generic;
using System.Linq;
using UnityEngine; using Game.EventManagement;
using UnityEngine.UI;

namespace Game
{
    /// <summary>
    /// Locked areas are special cell features that represent locked sections of the grid. They require certain objectives to be completed before
    /// they are unlocked and can accept elements. They can be used to create progression and challenge in the game by blocking access to certain areas of the grid until specific conditions are met.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Cell Feature/Locked Area...")]
    public class LockedAreaFeature : CellFeature
    {
        public ObjectiveUINode objectiveNodePrefab;
        public LayoutGroup lockedAreaCanvasParentPrefab;

        public override bool AcceptElements => false;

        public static void InitializeForGrid(Match3Grid grid)
        {
            LockedAreaRuntime.InitializeForGrid(grid);
        }

        public override void OnElementMatchedOverTheCell(Grid3D.GridCell cell, GridElement element)
        {
        }
        public override void OnElementMatchedAdjacentToTheCell(Grid3D.GridCell thisCell, Grid3D.GridCell matchedCell, GridElement element)
        {
        }
    }

    public class LockedAreaConfig
    {
        public int lockedAreaIndex;
        public LockedAreaFeature lockedAreaReference;

        private readonly Match3Grid grid;
        private readonly List<Grid3D.GridCell> lockedCells = new List<Grid3D.GridCell>();
        private readonly List<Vector2Int> lockedCellPositions = new List<Vector2Int>();
        private readonly List<ObjectiveUINode> objectiveNodes = new List<ObjectiveUINode>();
        private readonly List<Objective> activeObjectives = new List<Objective>();

        private Canvas worldCanvas;
        private LayoutGroup canvasLayoutGroup;

        public LockedAreaConfig(int lockedAreaIndex, LockedAreaFeature lockedAreaReference, Match3Grid grid)
        {
            this.lockedAreaIndex = lockedAreaIndex;
            this.lockedAreaReference = lockedAreaReference;
            this.grid = grid;
        }

        public void AddCell(Grid3D.GridCell cell)
        {
            if (cell == null || lockedCells.Contains(cell))
                return;

            lockedCells.Add(cell);
            lockedCellPositions.Add(cell.coordinates);
            cell.lockedAreaConfig = this;
        }

        public void InitializeVisuals()
        {
            CreateWorldCanvasAndLayout();
            RefreshObjectives();
            RebuildObjectiveNodes();
            UpdateObjectiveNodes();
        }

        public void RefreshObjectives()
        {
            activeObjectives.Clear();

            List<Objective> managerObjectives = ObjectiveManager.Instance != null ? ObjectiveManager.Instance.activeObjectives : null;
            if (managerObjectives == null)
                return;

            for (int i = 0; i < managerObjectives.Count; i++)
            {
                Objective objective = managerObjectives[i];
                if (objective == null || !objective.tiedToLockedArea || objective.lockedAreaIndex != lockedAreaIndex)
                    continue;

                activeObjectives.Add(objective);
            }
        }

        public void RebuildObjectiveNodes()
        {
            objectiveNodes.ForEach(node =>
            {
                if (node != null)
                    Object.Destroy(node.gameObject);
            });
            objectiveNodes.Clear();

            if (canvasLayoutGroup == null || lockedAreaReference == null || lockedAreaReference.objectiveNodePrefab == null)
                return;

            for (int i = 0; i < activeObjectives.Count; i++)
            {
                Objective objective = activeObjectives[i];
                ObjectiveUINode node = Object.Instantiate(lockedAreaReference.objectiveNodePrefab, canvasLayoutGroup.transform);
                node.Initialize(objective);
                objectiveNodes.Add(node);
            }
        }

        public void UpdateObjectiveNodes()
        {
            for (int i = 0; i < objectiveNodes.Count; i++)
            {
                ObjectiveUINode node = objectiveNodes[i];
                if (node == null || node.referenceObjective == null || ObjectiveManager.Instance == null)
                    continue;

                int currentCount = ObjectiveManager.Instance.GetCurrentCount(node.referenceObjective);
                node.UpdateNode(currentCount);
            }
        }

        public bool ShouldUnlock()
        {
            if (activeObjectives.Count == 0)
                return true;

            for (int i = 0; i < activeObjectives.Count; i++)
            {
                if (!activeObjectives[i].isCompleted)
                    return false;
            }

            return true;
        }

        public void Unlock()
        {
            for (int i = 0; i < lockedCells.Count; i++)
            {
                Grid3D.GridCell cell = lockedCells[i];
                if (cell == null || cell.cellFeature != lockedAreaReference)
                    continue;

                cell.cellFeature = null;
                cell.lockedAreaConfig = null;
                cell.cellFeatureGroupHealth = 0;
                cell.cellFeatureGroupMaxHealth = 0;
                cell.cellFeatureGroupIndex = 0;

                grid.PlayCellFeatureDestroyEffectAt(lockedAreaReference, cell.coordinates);
                grid.RefreshCellFeatureVisualAt(cell.coordinates);
            }

            DestroyVisuals();
            grid.StartCoroutine(grid.ApplyGravityPublic());
        }

        public void DestroyVisuals()
        {
            objectiveNodes.ForEach(node =>
            {
                if (node != null)
                    Object.Destroy(node.gameObject);
            });
            objectiveNodes.Clear();

            if (canvasLayoutGroup != null)
                Object.Destroy(canvasLayoutGroup.gameObject);
            canvasLayoutGroup = null;

            if (worldCanvas != null)
                Object.Destroy(worldCanvas.gameObject);
            worldCanvas = null;
        }

        private void CreateWorldCanvasAndLayout()
        {
            if (grid == null || lockedCellPositions.Count == 0 || lockedAreaReference == null || lockedAreaReference.lockedAreaCanvasParentPrefab == null)
                return;

            GameObject canvasObj = new GameObject($"LockedAreaCanvas_{lockedAreaIndex}", typeof(RectTransform), typeof(Canvas));
            worldCanvas = canvasObj.GetComponent<Canvas>();
            worldCanvas.renderMode = RenderMode.WorldSpace;
            worldCanvas.sortingLayerName = "Default";
            worldCanvas.worldCamera = Camera.main;
            worldCanvas.sortingOrder = 10;

            RectTransform canvasRect = worldCanvas.GetComponent<RectTransform>();
            canvasRect.SetParent(grid.transform, false);
            canvasRect.localRotation = Quaternion.identity;
            canvasRect.localScale = Vector3.one * 0.01f;

            Bounds bounds = GetLockedAreaBounds();
            canvasRect.position = bounds.center;

            float width = Mathf.Max(100f, bounds.size.x * 100f);
            float height = Mathf.Max(100f, bounds.size.y * 100f);
            canvasRect.sizeDelta = new Vector2(width, height);

            canvasLayoutGroup = Object.Instantiate(lockedAreaReference.lockedAreaCanvasParentPrefab, canvasRect);

            RectTransform layoutRect = canvasLayoutGroup.transform as RectTransform;
            if (layoutRect != null)
            {
                layoutRect.anchorMin = new Vector2(0.5f, 0.5f);
                layoutRect.anchorMax = new Vector2(0.5f, 0.5f);
                layoutRect.pivot = new Vector2(0.5f, 0.5f);
                layoutRect.anchoredPosition = Vector2.zero;
            }
        }

        private Bounds GetLockedAreaBounds()
        {
            Vector3 firstPos = grid.GetWorldPosition(lockedCellPositions[0]);
            Vector3 min = firstPos;
            Vector3 max = firstPos;

            for (int i = 1; i < lockedCellPositions.Count; i++)
            {
                Vector3 pos = grid.GetWorldPosition(lockedCellPositions[i]);
                min = Vector3.Min(min, pos);
                max = Vector3.Max(max, pos);
            }

            Vector3 cellSize = EstimateCellSize();
            min -= cellSize * 0.5f;
            max += cellSize * 0.5f;

            Bounds bounds = new Bounds((min + max) * 0.5f, max - min);
            return bounds;
        }

        private Vector3 EstimateCellSize()
        {
            Vector2Int first = lockedCellPositions[0];

            Grid3D.GridCell rightCell = grid.GetCellPublic(first + Vector2Int.right);
            if (rightCell != null)
            {
                float width = Mathf.Abs(grid.GetWorldPosition(rightCell.coordinates).x - grid.GetWorldPosition(first).x);
                if (width > 0.001f)
                    return new Vector3(width, width, 0f);
            }

            Grid3D.GridCell upCell = grid.GetCellPublic(first + Vector2Int.up);
            if (upCell != null)
            {
                float height = Mathf.Abs(grid.GetWorldPosition(upCell.coordinates).y - grid.GetWorldPosition(first).y);
                if (height > 0.001f)
                    return new Vector3(height, height, 0f);
            }

            return Vector3.one;
        }
    }

    internal static class LockedAreaRuntime
    {
        private class RuntimeState
        {
            private readonly Dictionary<int, LockedAreaConfig> configs = new Dictionary<int, LockedAreaConfig>();
            private Match3Grid grid;

            public void Initialize(Match3Grid grid)
            {
                this.grid = grid;
                Clear();

                if (grid == null)
                    return;

                Vector2Int gridSize = grid.GridSize;
                for (int x = 0; x < gridSize.x; x++)
                {
                    for (int y = 0; y < gridSize.y; y++)
                    {
                        Vector2Int position = new Vector2Int(x, y);
                        Grid3D.GridCell cell = grid.GetCellPublic(position);
                        if (cell?.cellFeature is not LockedAreaFeature lockedAreaFeature)
                            continue;

                        int lockedAreaIndex = cell.cellFeatureGroupIndex;
                        if (!configs.TryGetValue(lockedAreaIndex, out LockedAreaConfig config))
                        {
                            config = new LockedAreaConfig(lockedAreaIndex, lockedAreaFeature, grid);
                            configs.Add(lockedAreaIndex, config);
                        }

                        config.AddCell(cell);
                    }
                }

                foreach (LockedAreaConfig config in configs.Values)
                {
                    config.InitializeVisuals();
                }

                EventManager.StartListening(GameEvent.OBJECTIVE_PROGRESS_UPDATED, OnObjectiveProgressUpdated);
                EventManager.StartListening(GameEvent.OBJECTIVES_INITIALIZED, OnObjectivesInitialized);
                EventManager.StartListening(GameEvent.LEVEL_COMPLETED, OnLevelEnded);
                EventManager.StartListening(GameEvent.LEVEL_FAILED, OnLevelEnded);

                RefreshAndUnlockCompletedAreas();
            }

            public void Clear()
            {
                EventManager.StopListening(GameEvent.OBJECTIVE_PROGRESS_UPDATED, OnObjectiveProgressUpdated);
                EventManager.StopListening(GameEvent.OBJECTIVES_INITIALIZED, OnObjectivesInitialized);
                EventManager.StopListening(GameEvent.LEVEL_COMPLETED, OnLevelEnded);
                EventManager.StopListening(GameEvent.LEVEL_FAILED, OnLevelEnded);

                foreach (LockedAreaConfig config in configs.Values)
                {
                    config.DestroyVisuals();
                }

                configs.Clear();
            }

            private void OnObjectivesInitialized(EventParam param)
            {
                foreach (LockedAreaConfig config in configs.Values)
                {
                    config.RefreshObjectives();
                    config.RebuildObjectiveNodes();
                }

                RefreshAndUnlockCompletedAreas();
            }

            private void OnObjectiveProgressUpdated(EventParam param)
            {
                RefreshAndUnlockCompletedAreas();
            }

            private void OnLevelEnded(EventParam param)
            {
                Clear();
            }

            private void RefreshAndUnlockCompletedAreas()
            {
                List<int> completedIndexes = new List<int>();

                foreach (var pair in configs)
                {
                    LockedAreaConfig config = pair.Value;
                    config.UpdateObjectiveNodes();
                    if (config.ShouldUnlock())
                    {
                        completedIndexes.Add(pair.Key);
                    }
                }

                for (int i = 0; i < completedIndexes.Count; i++)
                {
                    int index = completedIndexes[i];
                    if (!configs.TryGetValue(index, out LockedAreaConfig config))
                        continue;

                    config.Unlock();
                    configs.Remove(index);
                }
            }
        }

        private static readonly Dictionary<int, RuntimeState> runtimeByGridId = new Dictionary<int, RuntimeState>();

        public static void InitializeForGrid(Match3Grid grid)
        {
            if (grid == null)
                return;

            int gridId = grid.GetInstanceID();
            if (!runtimeByGridId.TryGetValue(gridId, out RuntimeState runtime))
            {
                runtime = new RuntimeState();
                runtimeByGridId.Add(gridId, runtime);
            }

            runtime.Initialize(grid);
        }
    }
}
