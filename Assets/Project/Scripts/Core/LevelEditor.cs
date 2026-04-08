using System.Collections.Generic;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Game
{
    [ShowOdinSerializedPropertiesInInspector, InlineEditor]
    [CreateAssetMenu(fileName = "New Level", menuName = "Game/New Level")]
    public class LevelEditor : SerializedScriptableObject
    {
        [ GUIColor("green")]
        public Grid3D.LevelCreationMode levelCreationMode = Grid3D.LevelCreationMode.LevelEditor;

        [ShowIf(nameof(UseProcedural))]
        public Vector2Int gridSize = new Vector2Int(8, 8);
        public List<Objective> objectives = new List<Objective>();
        public GameEvent specialWinEvent;
        public ElementData targetElement;
        public int timer = -1;
        [ShowIf(nameof(UseProcedural))]
        public Grid3D.ProceduralGenerationSettings proceduralGeneration = new Grid3D.ProceduralGenerationSettings();

        [Tooltip("Pool used for randomly generated refill elements during gameplay.")]
        [SerializeField] private List<ElementData> elementPool = new List<ElementData>();

        [HideIf(nameof(UseProcedural))]
        [TableMatrix(DrawElementMethod = nameof(DrawGridCells), SquareCells = true)]
        [OdinSerialize] private Grid3D.GridCell[,] gridCells;

        public Vector2Int GridSize => gridSize;
        public List<ElementData> ElementPool => elementPool;

        private bool UseProcedural => levelCreationMode == Grid3D.LevelCreationMode.Procedural;

#if UNITY_EDITOR
        [Button]
        public void InitializeArray()
        {
            if (gridSize.x <= 0 || gridSize.y <= 0)
            {
                gridCells = new Grid3D.GridCell[0, 0];
                MarkDirty();
                return;
            }

            gridCells = new Grid3D.GridCell[gridSize.x, gridSize.y];
            for (int x = 0; x < gridSize.x; x++)
            {
                for (int y = 0; y < gridSize.y; y++)
                {
                    gridCells[x, y] = new Grid3D.GridCell
                    {
                        coordinates = new Vector2Int(x, y),
                        cellType = Grid3D.CellType.Normal
                    };
                }
            }

            MarkDirty();
        }

        private void MarkDirty()
        {
            EditorUtility.SetDirty(this);
        }
#endif

        public Grid3D.GridCell[,] CreateRuntimeGrid()
        {
            EnsureGridCells();

            Grid3D.GridCell[,] runtimeCells = new Grid3D.GridCell[gridSize.x, gridSize.y];
            for (int x = 0; x < gridSize.x; x++)
            {
                for (int y = 0; y < gridSize.y; y++)
                {
                    Grid3D.GridCell sourceCell = gridCells[x, y];
                    Grid3D.CellType cellType = sourceCell != null ? sourceCell.cellType : Grid3D.CellType.Empty;
                    GridElementInfo elementInfo = cellType == Grid3D.CellType.Normal &&
                                                  sourceCell != null &&
                                                  sourceCell.elementInfo != null &&
                                                  sourceCell.elementInfo.elementData != null
                        ? new GridElementInfo
                        {
                            elementData = sourceCell.elementInfo.elementData,
                            isSparkling = sourceCell.elementInfo.isSparkling,
                            isHidden = sourceCell.elementInfo.isHidden,
                            powerUpType = sourceCell.elementInfo.powerUpType
                        }
                        : null;

                    runtimeCells[x, y] = new Grid3D.GridCell
                    {
                        coordinates = new Vector2Int(x, y),
                        cellType = cellType,
                        elementInfo = elementInfo
                    };
                }
            }

            return runtimeCells;
        }

        private void EnsureGridCells()
        {
            if (gridSize.x <= 0 || gridSize.y <= 0)
            {
                gridCells = new Grid3D.GridCell[0, 0];
                return;
            }

            if (gridCells == null ||
                gridCells.GetLength(0) != gridSize.x ||
                gridCells.GetLength(1) != gridSize.y)
            {
                Grid3D.GridCell[,] newCells = new Grid3D.GridCell[gridSize.x, gridSize.y];

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
                        gridCells[x, y] = new Grid3D.GridCell { cellType = Grid3D.CellType.Normal };
                    }

                    gridCells[x, y].coordinates = new Vector2Int(x, y);
                }
            }
        }

#if UNITY_EDITOR
        private Grid3D.GridCell DrawGridCells(Rect rect, Grid3D.GridCell value)
        {
            if (value == null)
            {
                value = new Grid3D.GridCell { cellType = Grid3D.CellType.Normal };
            }

            Color emptyCellColor = new Color(0.8f, 0.8f, 0.8f, 0.5f);
            Color normalCellColor = new Color(0.5f, 0.5f, 1f, 0.5f);
            Color breakableWallColor = new Color(1f, 0.5f, 0.2f, 0.8f);
            Rect elementRect = new Rect(rect.x + rect.width * 0.1f, rect.y + rect.height * 0.1f, rect.width * 0.8f, rect.height * 0.8f);

            switch (value.cellType)
            {
                case Grid3D.CellType.Normal:
                    EditorGUI.DrawRect(rect, normalCellColor);
                    break;
                case Grid3D.CellType.Empty:
                    EditorGUI.DrawRect(rect, emptyCellColor);
                    break;
                case Grid3D.CellType.BreakableWall:
                    EditorGUI.DrawRect(rect, breakableWallColor);
                    break;
            }

            if (value.cellType != Grid3D.CellType.Normal)
            {
                value.elementInfo = null;
            }

            if (value.elementInfo != null && value.elementInfo.elementData != null)
            {
                Sprite icon = value.elementInfo.elementData.displayIcon;
                if (icon != null && icon.texture != null)
                {
                    Rect spriteRect = icon.textureRect;
                    Rect uv = new Rect(
                        spriteRect.x / icon.texture.width,
                        spriteRect.y / icon.texture.height,
                        spriteRect.width / icon.texture.width,
                        spriteRect.height / icon.texture.height);
                    GUI.DrawTextureWithTexCoords(elementRect, icon.texture, uv, true);
                }
            }

            if (rect.Contains(Event.current.mousePosition))
            {
                if (Event.current.type == EventType.KeyDown)
                {
                    if (Event.current.keyCode == KeyCode.E)
                    {
                        value.cellType = Grid3D.CellType.Empty;
                        value.elementInfo = null;
                        MarkDirty();
                        Event.current.Use();
                    }
                    else if (Event.current.keyCode == KeyCode.N)
                    {
                        value.cellType = Grid3D.CellType.Normal;
                        MarkDirty();
                        Event.current.Use();
                    }
                    else if (Event.current.keyCode == KeyCode.B)
                    {
                        value.cellType = Grid3D.CellType.BreakableWall;
                        value.elementInfo = null;
                        MarkDirty();
                        Event.current.Use();
                    }
                }
                else if (Event.current.type == EventType.MouseDown && Event.current.button == 1)
                {
                    GenericMenu menu = new GenericMenu();
                    string[] elementGuids = AssetDatabase.FindAssets("t:ElementData");
                    foreach (string guid in elementGuids)
                    {
                        string path = AssetDatabase.GUIDToAssetPath(guid);
                        ElementData elementData = AssetDatabase.LoadAssetAtPath<ElementData>(path);
                        if (elementData != null)
                        {
                            menu.AddItem(new GUIContent(elementData.name), false, () =>
                            {
                                value.cellType = Grid3D.CellType.Normal;
                                value.elementInfo = new GridElementInfo { elementData = elementData };
                                MarkDirty();
                            });
                        }
                    }
                    menu.ShowAsContext();
                    Event.current.Use();
                }
                else if (Event.current.type == EventType.KeyDown && Event.current.keyCode >= KeyCode.Alpha1 && Event.current.keyCode <= KeyCode.Alpha9)
                {
                    int index = Event.current.keyCode - KeyCode.Alpha1;
                    string[] elementGuids = AssetDatabase.FindAssets("t:ElementData");
                    if (index < elementGuids.Length)
                    {
                        string path = AssetDatabase.GUIDToAssetPath(elementGuids[index]);
                        ElementData elementData = AssetDatabase.LoadAssetAtPath<ElementData>(path);
                        if (elementData != null)
                        {
                            value.cellType = Grid3D.CellType.Normal;
                            value.elementInfo = new GridElementInfo { elementData = elementData };
                            MarkDirty();
                        }
                    }
                    Event.current.Use();
                }
            }

            return value;
        }
#else
        private Grid3D.GridCell DrawGridCells(Rect rect, Grid3D.GridCell value)
        {
            return value;
        }
#endif
    }
}
