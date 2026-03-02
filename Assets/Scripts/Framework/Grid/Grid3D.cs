using Sirenix.OdinInspector;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

namespace Game
{
    public abstract class Grid3D : MonoBehaviour, IBusyChecker
    {
        [FoldoutGroup("Grid 3D")]
        [SerializeField] protected GridElement gridElementPrefab;
        [FoldoutGroup("Grid 3D")]
        [Tooltip("Dictionary of cell types indexed by their cell positions. Used for tile generation (i.e. neighbors of empty cells will be generated ")]
        [SerializeField] protected SerializedDictionary<Vector3Int, CellType> gridCellTypes;
        [FoldoutGroup("Grid 3D")]
        [Tooltip("Dictionary of grid elements indexed by their cell positions")]
        [SerializeField] protected SerializedDictionary<Vector3Int, GridElementInfo> gridElements = new();
        [FoldoutGroup("Grid 3D")]
        [Tooltip("Tile data used for generation, based on the cell positions in the grid")]
        [SerializeField] protected TileData tileGenerationData;
        [FoldoutGroup("Grid 3D")]
        [Tooltip("Parent transform for generated tiles")]
        [SerializeField] protected Transform parent;
        [FoldoutGroup("Grid 3D")]
        [Tooltip("Size of the grid in number of cells")]
        [SerializeField] protected Vector3Int gridSize;
        [FoldoutGroup("Grid 3D")]
        [Tooltip("If an axis is not selected, tiles will not be generated when axis is greater than 0")]
        [SerializeField] protected Axis tileGeneratedAxes = Axis.X | Axis.Y;
        [FoldoutGroup("Grid 3D")]
        [Tooltip("If an axis is selected, tiles will be generated in reverse direction on that axis. Y is reversed by default.")]
        [SerializeField] protected Axis reversedAxes = Axis.Y;

        public List<BusyReason> BusyReasons { get; } = new();

        protected Dictionary<Vector3Int, Transform> generatedTiles = new();
        protected List<GridElement> generatedElements = new();

        protected virtual void Start()
        {
            PreInit();
            Init();
            PostInit();
        }

        private void Init()
        {
            bool[,] generationData = GetGenerationData();
            if(tileGenerationData)
            {
                generatedTiles = tileGenerationData.Generate(generationData, parent.position, TileData.DrawStartingCorner.TopLeft, parent);
                GenerateElements();
            }
        }

        protected virtual void GenerateElements()
        {
            foreach (var kvp in gridElements)
            {
                Transform tile = generatedTiles[kvp.Key];
                GridElement element = Instantiate(gridElementPrefab, tile.position, Quaternion.identity, tile);
                element.elementInfo = kvp.Value;
                generatedElements.Add(element);
                element.InitElement(this, element.elementInfo);
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
            bool[,] generationData;

            // Populate generationData based on gridCellTypes. 1 if occupied or blocked, 0 if empty.
            generationData = new bool[gridSize.x, gridSize.z];
            for (int x = 0; x < gridSize.x; x++)
            {
                for (int z = 0; z < gridSize.z; z++)
                {
                    Vector3Int cellPos = new Vector3Int(x, 0, z);
                    if (gridCellTypes.ContainsKey(cellPos))
                    {
                        generationData[x, z] = gridCellTypes[cellPos] != CellType.Empty;
                    }
                    else
                    {
                        generationData[x, z] = false; // Default to empty if not specified
                    }
                }
            }

            return generationData;
        }

        [System.Flags]
        public enum Axis
        {
            X = 1 << 0,
            Y = 1 << 1,
            Z = 1 << 2,
            All = X | Y | Z
        }

        public enum CellType
        {
            Empty,
            Normal,
            Blocked
        }
    }
}
