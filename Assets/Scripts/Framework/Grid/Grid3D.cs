using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Rendering;

namespace Game
{
    public abstract partial class Grid3D : SerializedMonoBehaviour, IBusyChecker
    {
        private LevelCreationMode levelCreationMode = LevelCreationMode.LevelEditor;
        private LevelEditor levelEditor;
        private ProceduralGenerationSettings proceduralGeneration = new ProceduralGenerationSettings();

        [FoldoutGroup("Grid 3D")]
        [SerializeField] protected GridElement gridElementPrefab;
        [FoldoutGroup("Grid 3D")]
        [Tooltip("Matrix of grid cells indexed by their coordinates")]
        [SerializeField] protected GridCell[,] gridCells;
        [FoldoutGroup("Grid 3D")]
        [Tooltip("Tile data used for generation, based on the cell positions in the grid")]
        [SerializeField] protected TileData tileGenerationData;
        [FoldoutGroup("Grid 3D")]
        [Tooltip("Parent transform for generated tiles")]
        [SerializeField] protected Transform parent;
        [FoldoutGroup("Grid 3D")]
        [Tooltip("Size of the grid in number of cells")]
        [HideInInspector]
        [SerializeField] protected Vector2Int gridSize;
        [FoldoutGroup("Grid 3D")]
        [Tooltip("If an axis is not selected, tiles will not be generated when axis is greater than 0")]
        [SerializeField] protected Axis tileGeneratedAxes = Axis.X | Axis.Y;
        [FoldoutGroup("Grid 3D")]
        [Tooltip("If an axis is selected, tiles will be generated in reverse direction on that axis. Y is reversed by default.")]
        [SerializeField] protected Axis reversedAxes = Axis.Y;



        public List<BusyReason> BusyReasons { get; } = new();

        protected Dictionary<Vector2Int, GridCellController> generatedTiles = new();
        protected List<GridElement> generatedElements = new();
        private bool isInitialized;

        protected virtual void Start()
        {
            PreInit();
            Init();
            PostInit();
        }

        public void ConfigureLevelSettings(LevelCreationMode mode, LevelEditor editor, ProceduralGenerationSettings settings, Vector2Int proceduralGridSize)
        {
            levelCreationMode = mode;
            levelEditor = editor;
            proceduralGeneration = settings ?? new ProceduralGenerationSettings();

            if (mode == LevelCreationMode.Procedural)
            {
                gridSize = proceduralGridSize;
            }
        }

        private void Init()
        {
            InitializeGridCells();
            EnsureGridCells();
            bool[,] generationData = GetGenerationData();
            if (tileGenerationData)
            {
                generatedTiles = tileGenerationData.Generate(generationData, parent.position, TileData.DrawStartingCorner.TopLeft, parent, false, gridCells);
                GenerateElements();
            }
            
            EventManager.TriggerEvent(GameEvent.GRID_INITIALIZED, new EventParam(
                paramInt: gridSize.x * gridSize.y
            ));

            isInitialized = true;
        }

        public void RebuildWithSettings(LevelCreationMode mode, LevelEditor editor, ProceduralGenerationSettings settings, Vector2Int proceduralGridSize)
        {
            bool hadPreviousCenter = TryGetGeneratedTilesCenter(out Vector3 previousCenter);

            ConfigureLevelSettings(mode, editor, settings, proceduralGridSize);
            ClearGeneratedRuntimeObjects();

            Init();

            if (hadPreviousCenter && parent != null && TryGetGeneratedTilesCenter(out Vector3 newCenter))
            {
                Vector3 offset = previousCenter - newCenter;
                parent.position += offset;
            }
        }

        private bool TryGetGeneratedTilesCenter(out Vector3 center)
        {
            center = Vector3.zero;

            if (generatedTiles == null || generatedTiles.Count == 0)
                return false;

            bool hasAny = false;
            Vector3 min = Vector3.zero;
            Vector3 max = Vector3.zero;

            foreach (var kvp in generatedTiles)
            {
                GridCellController tile = kvp.Value;
                if (tile == null)
                    continue;

                Vector3 pos = tile.transform.position;
                if (!hasAny)
                {
                    min = pos;
                    max = pos;
                    hasAny = true;
                }
                else
                {
                    min = Vector3.Min(min, pos);
                    max = Vector3.Max(max, pos);
                }
            }

            if (!hasAny)
                return false;

            center = (min + max) * 0.5f;
            return true;
        }

        private void ClearGeneratedRuntimeObjects()
        {
            foreach (var tile in generatedTiles)
            {
                if (tile.Value != null)
                    Destroy(tile.Value.gameObject);
            }

            for (int i = 0; i < generatedElements.Count; i++)
            {
                if (generatedElements[i] != null)
                    Destroy(generatedElements[i].gameObject);
            }

            generatedTiles.Clear();
            generatedElements.Clear();
            isInitialized = false;
        }

        private void InitializeGridCells()
        {
            if (UseLevelEditor)
            {
                ApplyLevelEditor();
                return;
            }

            ApplyProceduralGeneration();
        }

        private void ApplyLevelEditor()
        {
            if (levelEditor == null)
            {
                gridCells = new GridCell[0, 0];
                return;
            }

            gridSize = levelEditor.GridSize;
            gridCells = levelEditor.CreateRuntimeGrid();
        }

        private void ApplyProceduralGeneration()
        {
            if (gridSize.x <= 0 || gridSize.y <= 0)
            {
                gridCells = new GridCell[0, 0];
                return;
            }

            gridCells = new GridCell[gridSize.x, gridSize.y];
            System.Random random = proceduralGeneration.CreateRandom();
            List<ElementData> configuredElementPool = GetConfiguredElementPool();

            float emptyChance = Mathf.Clamp01(proceduralGeneration.emptyCellChance);
            float hiddenBoxChance = Mathf.Clamp01(proceduralGeneration.hiddenBoxChance);

            // First pass: fill all cells as Normal or Empty (no breakable walls yet)
            for (int x = 0; x < gridSize.x; x++)
            {
                for (int y = 0; y < gridSize.y; y++)
                {
                    double roll = random.NextDouble();
                    CellType cellType = roll < emptyChance ? CellType.Empty : CellType.Normal;

                    ElementData elementData = null;
                    bool isHidden = false;
                    if (cellType == CellType.Normal && configuredElementPool.Count > 0)
                    {
                        int index = random.Next(configuredElementPool.Count);
                        elementData = configuredElementPool[index];
                        isHidden = random.NextDouble() < hiddenBoxChance;
                    }

                    gridCells[x, y] = new GridCell
                    {
                        coordinates = new Vector2Int(x, y),
                        cellType = cellType,
                        elementInfo = elementData != null ? new GridElementInfo { elementData = elementData, isHidden = isHidden } : null
                    };
                }
            }

            // Second pass: place breakable walls according to the chosen placement mode.
            int baseCount = Mathf.Max(0, proceduralGeneration.triangleWidth);

            switch (proceduralGeneration.breakableWallPlacementMode)
            {
                case BreakableWallPlacementMode.Flat:
                {
                    int wallsToPlace = baseCount;
                    for (int y = gridSize.y - 1; y >= 0 && wallsToPlace > 0; y--)
                    {
                        for (int x = 0; x < gridSize.x && wallsToPlace > 0; x++)
                        {
                            GridCell cell = gridCells[x, y];
                            if (cell != null && cell.cellType == CellType.Normal)
                            {
                                cell.cellType = CellType.BreakableWall;
                                cell.elementInfo = null;
                                wallsToPlace--;
                            }
                        }
                    }
                    break;
                }
                case BreakableWallPlacementMode.Triangle:
                {
                    int decrement = Mathf.Max(1, proceduralGeneration.triangleDecrement);
                    int rowQuota = baseCount;
                    for (int y = gridSize.y - 1; y >= 0 && rowQuota > 0; y--, rowQuota -= decrement)
                    {
                        int wallsThisRow = 0;
                        for (int x = 0; x < gridSize.x && wallsThisRow < rowQuota; x++)
                        {
                            GridCell cell = gridCells[x, y];
                            if (cell != null && cell.cellType == CellType.Normal)
                            {
                                cell.cellType = CellType.BreakableWall;
                                cell.elementInfo = null;
                                wallsThisRow++;
                            }
                        }
                    }
                    break;
                }
                case BreakableWallPlacementMode.Rectangle:
                {
                    int minX = Mathf.Clamp(Mathf.Min(proceduralGeneration.rectangleStart.x, proceduralGeneration.rectangleEnd.x), 0, gridSize.x - 1);
                    int maxX = Mathf.Clamp(Mathf.Max(proceduralGeneration.rectangleStart.x, proceduralGeneration.rectangleEnd.x), 0, gridSize.x - 1);
                    int minY = Mathf.Clamp(Mathf.Min(proceduralGeneration.rectangleStart.y, proceduralGeneration.rectangleEnd.y), 0, gridSize.y - 1);
                    int maxY = Mathf.Clamp(Mathf.Max(proceduralGeneration.rectangleStart.y, proceduralGeneration.rectangleEnd.y), 0, gridSize.y - 1);

                    for (int y = minY; y <= maxY; y++)
                    {
                        for (int x = minX; x <= maxX; x++)
                        {
                            GridCell cell = gridCells[x, y];
                            if (cell != null && cell.cellType == CellType.Normal)
                            {
                                cell.cellType = CellType.BreakableWall;
                                cell.elementInfo = null;
                            }
                        }
                    }
                    break;
                }
            }
        }

        protected virtual void GenerateElements()
        {
            EnsureGridCells();
            for (int x = 0; x < gridSize.x; x++)
            {
                for (int y = 0; y < gridSize.y; y++)
                {
                    GridCell cell = gridCells[x, y];
                    if (cell == null || cell.cellType != CellType.Normal || cell.elementInfo == null)
                    {
                        continue;
                    }

                    if (!generatedTiles.TryGetValue(cell.coordinates, out GridCellController tile))
                    {
                        continue;
                    }

                    GridElement element = Instantiate(gridElementPrefab, tile.transform.position, Quaternion.identity, tile.transform);
                    element.elementInfo = cell.elementInfo;
                    generatedElements.Add(element);
                    element.InitElement(this, element.elementInfo);

                    if (cell.elementInfo != null && cell.elementInfo.isHidden)
                    {
                        EventManager.TriggerEvent(GameEvent.HIDDEN_BOX_CREATED, new EventParam(
                            vectorList: new Vector3[] { new Vector3(cell.coordinates.x, cell.coordinates.y, 0f) },
                            paramScriptable: cell.elementInfo.elementData
                        ));
                    }
                }
            }
        }

        protected GridCell GetCell(Vector2Int cellPos)
        {
            EnsureGridCells();

            if (cellPos.x < 0 || cellPos.y < 0 || cellPos.x >= gridSize.x || cellPos.y >= gridSize.y)
            {
                return null;
            }

            return gridCells[cellPos.x, cellPos.y];
        }

        protected virtual void EnsureGridCells()
        {
            if (gridSize.x <= 0 || gridSize.y <= 0)
            {
                gridCells = new GridCell[0, 0];
                return;
            }

            if (gridCells == null ||
                gridCells.GetLength(0) != gridSize.x ||
                gridCells.GetLength(1) != gridSize.y)
            {
                GridCell[,] newCells = new GridCell[gridSize.x, gridSize.y];

                if (gridCells != null)
                {
                    int maxX = Mathf.Min(gridCells.GetLength(0), gridSize.x);
                    int maxY = Mathf.Min(gridCells.GetLength(1), gridSize.y);

                    for (int x = 0; x < maxX; x++)
                    {
                        for (int y = 0; y < maxY; y++)
                        {
                            newCells[x, y] = gridCells[x, y];
                        }
                    }
                }

                gridCells = newCells;
            }

            for (int x = 0; x < gridSize.x; x++)
            {
                for (int y = 0; y < gridSize.y; y++)
                {
                    if (gridCells[x, y] == null)
                    {
                        gridCells[x, y] = new GridCell();
                    }

                    gridCells[x, y].coordinates = new Vector2Int(x, y);
                }
            }
        }

        /// <summary>
        /// Called on Start() before any base class initialization.
        /// </summary>
        public abstract void PreInit();
        /// <summary>
        /// Called on Start() after all base class initializations.
        /// </summary>
        public abstract void PostInit();
        public virtual bool[,] GetGenerationData()
        {
            EnsureGridCells();
            bool[,] generationData;

            // Populate generationData based on gridCells. 1 if occupied or blocked, 0 if empty.
            generationData = new bool[gridSize.x, gridSize.y];
            for (int x = 0; x < gridSize.x; x++)
            {
                for (int y = 0; y < gridSize.y; y++)
                {
                    GridCell cell = GetCell(new Vector2Int(x, y));
                    generationData[x, y] = cell != null && cell.cellType != CellType.Empty;
                }
            }

            return generationData;
        }

        protected bool UseLevelEditor => levelCreationMode == LevelCreationMode.LevelEditor;
        protected bool UseProcedural => levelCreationMode == LevelCreationMode.Procedural;
        protected LevelEditor ActiveLevelEditor => levelEditor;

        protected List<ElementData> GetConfiguredElementPool()
        {
            List<ElementData> pool = new List<ElementData>();

            if (ActiveLevelEditor != null && ActiveLevelEditor.ElementPool != null)
            {
                for (int i = 0; i < ActiveLevelEditor.ElementPool.Count; i++)
                {
                    ElementData data = ActiveLevelEditor.ElementPool[i];
                    if (data != null && !pool.Contains(data))
                        pool.Add(data);
                }
            }

            if (pool.Count == 0 && proceduralGeneration != null && proceduralGeneration.elementPool != null)
            {
                for (int i = 0; i < proceduralGeneration.elementPool.Count; i++)
                {
                    ElementData data = proceduralGeneration.elementPool[i];
                    if (data != null && !pool.Contains(data))
                        pool.Add(data);
                }
            }

            return pool;
        }

        public void OnSpawn()
        {
        }

        public void OnDespawn()
        {
        }

        [System.Serializable]
        public class ProceduralGenerationSettings
        {
            public bool useRandomSeed = true;
            public int seed;
            [Range(0f, 1f)]
            public float emptyCellChance;
            public BreakableWallPlacementMode breakableWallPlacementMode;
            [Min(0)]
            [LabelText("Wall Count / Triangle Width")]
            [HideIf(nameof(breakableWallPlacementMode), BreakableWallPlacementMode.Rectangle)]
            public int triangleWidth;
            [Min(1)]
            [ShowIf(nameof(breakableWallPlacementMode), BreakableWallPlacementMode.Triangle)]
            public int triangleDecrement = 1;
            [ShowIf(nameof(breakableWallPlacementMode), BreakableWallPlacementMode.Rectangle)]
            public Vector2Int rectangleStart;
            [ShowIf(nameof(breakableWallPlacementMode), BreakableWallPlacementMode.Rectangle)]
            public Vector2Int rectangleEnd;
            [Range(0f, 1f)]
            public float hiddenBoxChance;
            [HideInInspector]
            public List<ElementData> elementPool = new List<ElementData>();

            public System.Random CreateRandom()
            {
                return useRandomSeed ? new System.Random() : new System.Random(seed);
            }
        }

        public enum BreakableWallPlacementMode
        {
            /// <summary>Fills exactly triangleWidth cells from the bottom row upward, left to right.</summary>
            Flat,
            /// <summary>Fills triangleWidth cells on the bottom row, decreasing by triangleDecrement each row up, forming a triangle.</summary>
            Triangle,
            /// <summary>Fills every Normal cell inside the rectangle defined by rectangleStart and rectangleEnd (inclusive).</summary>
            Rectangle,
        }

        public enum LevelCreationMode
        {
            LevelEditor,
            Procedural
        }

        [System.Serializable]
        public class GridCell
        {
            public Vector2Int coordinates;
            public CellType cellType;
            public GridElementInfo elementInfo;
            public CellFeature cellFeature;
            public ElementData breakableWallElementCondition;
            public int cellFeatureGroupIndex;
            public int cellFeatureGroupHealth;
            public int cellFeatureGroupMaxHealth;
        }

        [System.Flags]
        public enum Axis
        {
            X = 1 << 0,
            Y = 1 << 1,
            All = X | Y
        }

        public enum CellType
        {
            Empty,
            Normal,
            BreakableWall,
            UnbreakableWall,
        }
    }
}
