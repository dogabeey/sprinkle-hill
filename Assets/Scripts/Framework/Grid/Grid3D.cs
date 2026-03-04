using Sirenix.OdinInspector;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Game
{
    public abstract class Grid3D : SerializedMonoBehaviour, IBusyChecker
    {
        [FoldoutGroup("Grid 3D")]
        [SerializeField] protected GridElement gridElementPrefab;
        [FoldoutGroup("Grid 3D")]
        [Tooltip("Matrix of grid cells indexed by their coordinates")]
        [TableMatrix(DrawElementMethod = nameof(DrawGridCells), SquareCells = true)]
        [SerializeField] protected GridCell[,] gridCells;
        [FoldoutGroup("Grid 3D")]
        [Tooltip("Tile data used for generation, based on the cell positions in the grid")]
        [SerializeField] protected TileData tileGenerationData;
        [FoldoutGroup("Grid 3D")]
        [Tooltip("Parent transform for generated tiles")]
        [SerializeField] protected Transform parent;
        [FoldoutGroup("Grid 3D")]
        [Tooltip("Size of the grid in number of cells")]
        [SerializeField] protected Vector2Int gridSize;
        [FoldoutGroup("Grid 3D")]
        [Tooltip("If an axis is not selected, tiles will not be generated when axis is greater than 0")]
        [SerializeField] protected Axis tileGeneratedAxes = Axis.X | Axis.Y;
        [FoldoutGroup("Grid 3D")]
        [Tooltip("If an axis is selected, tiles will be generated in reverse direction on that axis. Y is reversed by default.")]
        [SerializeField] protected Axis reversedAxes = Axis.Y;



        public List<BusyReason> BusyReasons { get; } = new();

        protected Dictionary<Vector2Int, Transform> generatedTiles = new();
        protected List<GridElement> generatedElements = new();

        protected virtual void Start()
        {
            PreInit();
            Init();
            PostInit();
        }
        protected abstract GridCell DrawGridCells(Rect rect, GridCell value);

        private void Init()
        {
            EnsureGridCells();
            bool[,] generationData = GetGenerationData();
            if(tileGenerationData)
            {
                generatedTiles = tileGenerationData.Generate(generationData, parent.position, TileData.DrawStartingCorner.TopLeft, parent);
                GenerateElements();
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
                    if (cell == null || cell.elementInfo == null)
                    {
                        continue;
                    }

                    if (!generatedTiles.TryGetValue(cell.coordinates, out Transform tile))
                    {
                        continue;
                    }

                    GridElement element = Instantiate(gridElementPrefab, tile.position, Quaternion.identity, tile);
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
        }
    }
}
