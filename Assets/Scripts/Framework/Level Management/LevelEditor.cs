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
        public enum LevelLimitType
        {
            Timer,
            Moves
        }

        [ GUIColor("green")]
        public Grid3D.LevelCreationMode levelCreationMode = Grid3D.LevelCreationMode.LevelEditor;
        public Vector2Int gridSize = new Vector2Int(8, 8);
        public List<Objective> objectives = new List<Objective>();
        public GameEvent specialWinEvent;
        public ElementData targetElement;
        public LevelLimitType levelLimitType = LevelLimitType.Timer;
        [ShowIf(nameof(IsTimerLimit))]
        public int timer = -1;
        [ShowIf(nameof(IsMovesLimit))]
        public int moves = 20;
        [ShowIf(nameof(UseProcedural))]
        public Grid3D.ProceduralGenerationSettings proceduralGeneration = new Grid3D.ProceduralGenerationSettings();

        [Tooltip("Pool used for randomly generated refill elements during gameplay.")]
        [SerializeField] private List<ElementData> elementPool = new List<ElementData>();

        [ShowInInspector, ReadOnly, MultiLineProperty(8), PropertyOrder(-1), LabelText("Editor Shortcuts")]
        private string ShortcutLegend =>
            "Grid Cell Shortcuts\n" +
            "E: Set cell to Empty\n" +
            "N: Set cell to Normal\n" +
            "B: Set cell to Breakable Wall\n" +
            "U: Set cell to Unbreakable Wall\n" +
            "R: Assign random element from pool\n" +
            "1-9: Assign element by indexed asset list\n" +
            "Right Click: Open element selection menu";

        [HideIf(nameof(UseProcedural))]
        [TableMatrix(DrawElementMethod = nameof(DrawGridCells), SquareCells = true)]
        [OdinSerialize] private Grid3D.GridCell[,] gridCells;

        public Vector2Int GridSize => gridSize;
        public List<ElementData> ElementPool => elementPool;

        private bool UseProcedural => levelCreationMode == Grid3D.LevelCreationMode.Procedural;
        private bool IsTimerLimit => levelLimitType == LevelLimitType.Timer;
        private bool IsMovesLimit => levelLimitType == LevelLimitType.Moves;

        private static ElementPowerUpType ResolveElementPowerUpType(ElementData data)
        {
            return data != null && data.isCauldron ? ElementPowerUpType.Cauldron : ElementPowerUpType.None;
        }

        private static GridElementInfo CreateElementInfo(ElementData data)
        {
            if (data == null)
                return null;

            return new GridElementInfo
            {
                elementData = data,
                powerUpType = ResolveElementPowerUpType(data),
                cauldronProgress = 0
            };
        }

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
        [Button]
        public void PopulateRandomlyFromPool()
            {
            if (elementPool == null || elementPool.Count == 0)
            {
                Debug.LogError("Element pool is empty. Cannot populate grid.");
                return;
            }
            EnsureGridCells();
            for (int x = 0; x < gridSize.x; x++)
            {
                for (int y = 0; y < gridSize.y; y++)
                {
                    Grid3D.GridCell cell = gridCells[x, y];
                    if (cell != null && cell.cellType == Grid3D.CellType.Normal)
                    {
                        ElementData randomElement = elementPool[Random.Range(0, elementPool.Count)];
                        cell.elementInfo = CreateElementInfo(randomElement);
                    }
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
                            powerUpType = sourceCell.elementInfo.powerUpType == ElementPowerUpType.None
                                ? ResolveElementPowerUpType(sourceCell.elementInfo.elementData)
                                : sourceCell.elementInfo.powerUpType,
                            cauldronProgress = sourceCell.elementInfo.cauldronProgress
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
            Color unbreakableWallColor = new Color(0.35f, 0.35f, 0.35f, 0.9f);
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
                case Grid3D.CellType.UnbreakableWall:
                    EditorGUI.DrawRect(rect, unbreakableWallColor);
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
                    else if (Event.current.keyCode == KeyCode.U)
                    {
                        value.cellType = Grid3D.CellType.UnbreakableWall;
                        value.elementInfo = null;
                        MarkDirty();
                        Event.current.Use();
                    }
                    else if (Event.current.keyCode == KeyCode.R)
                    {
                        if (elementPool != null && elementPool.Count > 0)
                        {
                            ElementData randomElement = elementPool[Random.Range(0, elementPool.Count)];
                            if (randomElement != null)
                            {
                                value.cellType = Grid3D.CellType.Normal;
                                value.elementInfo = CreateElementInfo(randomElement);
                                MarkDirty();
                            }
                        }
                        Event.current.Use();
                    }
                }
                else if (Event.current.type == EventType.MouseDown && Event.current.button == 1)
                {
                    GenericMenu menu = new GenericMenu();

                    if (elementPool != null && elementPool.Count > 0)
                    {
                        menu.AddItem(new GUIContent("Random Element (Pool)"), false, () =>
                        {
                            ElementData randomElement = elementPool[Random.Range(0, elementPool.Count)];
                            if (randomElement != null)
                            {
                                value.cellType = Grid3D.CellType.Normal;
                                value.elementInfo = CreateElementInfo(randomElement);
                                MarkDirty();
                            }
                        });
                        menu.AddSeparator("");
                    }

                    string[] elementGuids = AssetDatabase.FindAssets("t:ElementData");
                    HashSet<ElementData> poolSet = new HashSet<ElementData>();
                    if (elementPool != null)
                    {
                        for (int i = 0; i < elementPool.Count; i++)
                        {
                            if (elementPool[i] != null)
                                poolSet.Add(elementPool[i]);
                        }
                    }

                    bool addedPoolElements = false;
                    for (int i = 0; i < elementPool.Count; i++)
                    {
                        ElementData pooledElement = elementPool[i];
                        if (pooledElement == null)
                            continue;

                        ElementData capturedPooledElement = pooledElement;
                        menu.AddItem(new GUIContent($"{capturedPooledElement.name}"), false, () =>
                        {
                            value.cellType = Grid3D.CellType.Normal;
                            value.elementInfo = CreateElementInfo(capturedPooledElement);
                            MarkDirty();
                        });
                        addedPoolElements = true;
                    }

                    if (addedPoolElements)
                        menu.AddSeparator("");

                    foreach (string guid in elementGuids)
                    {
                        string path = AssetDatabase.GUIDToAssetPath(guid);
                        ElementData elementData = AssetDatabase.LoadAssetAtPath<ElementData>(path);
                        if (elementData != null)
                        {
                            if (poolSet.Contains(elementData))
                                continue;

                            ElementData capturedElementData = elementData;
                            menu.AddItem(new GUIContent(elementData.name), false, () =>
                            {
                                value.cellType = Grid3D.CellType.Normal;
                                value.elementInfo = CreateElementInfo(capturedElementData);
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
                            value.elementInfo = CreateElementInfo(elementData);
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
