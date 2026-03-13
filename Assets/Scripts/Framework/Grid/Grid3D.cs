using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Rendering;

namespace Game
{
    public abstract class Grid3D : SerializedMonoBehaviour, IBusyChecker
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

            float emptyChance = Mathf.Clamp01(proceduralGeneration.emptyCellChance);
            float breakableWallChance = Mathf.Clamp01(proceduralGeneration.breakableWallChance);

            for (int x = 0; x < gridSize.x; x++)
            {
                for (int y = 0; y < gridSize.y; y++)
                {
                    double roll = random.NextDouble();
                    CellType cellType;
                    if (roll < emptyChance)
                    {
                        cellType = CellType.Empty;
                    }
                    else if (roll < emptyChance + breakableWallChance)
                    {
                        cellType = CellType.BreakableWall;
                    }
                    else
                    {
                        cellType = CellType.Normal;
                    }

                    ElementData elementData = null;
                    if (cellType == CellType.Normal && proceduralGeneration.elementPool != null && proceduralGeneration.elementPool.Count > 0)
                    {
                        int index = random.Next(proceduralGeneration.elementPool.Count);
                        elementData = proceduralGeneration.elementPool[index];
                    }

                    gridCells[x, y] = new GridCell
                    {
                        coordinates = new Vector2Int(x, y),
                        cellType = cellType,
                        elementInfo = elementData != null ? new GridElementInfo { elementData = elementData } : null
                    };
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

        private bool UseLevelEditor => levelCreationMode == LevelCreationMode.LevelEditor;
        private bool UseProcedural => levelCreationMode == LevelCreationMode.Procedural;

        [System.Serializable]
        public class ProceduralGenerationSettings
        {
            public bool useRandomSeed = true;
            public int seed;
            [Range(0f, 1f)]
            public float emptyCellChance;
            [Range(0f, 1f)]
            public float breakableWallChance;
            public List<ElementData> elementPool = new List<ElementData>();

            public System.Random CreateRandom()
            {
                return useRandomSeed ? new System.Random() : new System.Random(seed);
            }
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
        }
    }
}
