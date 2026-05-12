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

        [Range(1,3)] public int levelDifficulty = 1;
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
            "H: Toggle Hidden Element\n" +
            "U: Set cell to Unbreakable Wall\n" +
            "Shift+Z: Toggle Wafer Feature\n" +
            "Shift+X: Toggle Glass Feature\n" +
            "Shift+A: Toggle Electric Field Feature\n" +
            "0-9: Set Glass Group Index (while hovering a glass cell)\n" +
            "Shift+0-9: Set Glass Group Health (while hovering a glass cell)\n" +
            "R: Toggle random element marker\n" +
            "Ctrl+R: Assign random element from pool\n" +
            "Right Click: Open element/cell feature menu";

        private static bool TryGetNumericShortcutValue(KeyCode keyCode, out int value)
        {
            switch (keyCode)
            {
                case KeyCode.Alpha0:
                case KeyCode.Keypad0:
                    value = 0;
                    return true;
                case KeyCode.Alpha1:
                case KeyCode.Keypad1:
                    value = 1;
                    return true;
                case KeyCode.Alpha2:
                case KeyCode.Keypad2:
                    value = 2;
                    return true;
                case KeyCode.Alpha3:
                case KeyCode.Keypad3:
                    value = 3;
                    return true;
                case KeyCode.Alpha4:
                case KeyCode.Keypad4:
                    value = 4;
                    return true;
                case KeyCode.Alpha5:
                case KeyCode.Keypad5:
                    value = 5;
                    return true;
                case KeyCode.Alpha6:
                case KeyCode.Keypad6:
                    value = 6;
                    return true;
                case KeyCode.Alpha7:
                case KeyCode.Keypad7:
                    value = 7;
                    return true;
                case KeyCode.Alpha8:
                case KeyCode.Keypad8:
                    value = 8;
                    return true;
                case KeyCode.Alpha9:
                case KeyCode.Keypad9:
                    value = 9;
                    return true;
                default:
                    value = 0;
                    return false;
            }
        }

        [HideIf(nameof(UseProcedural))]
#if UNITY_EDITOR
        [TableMatrix(DrawElementMethod = nameof(DrawGridCells), SquareCells = true)]
#endif
        [OdinSerialize] private Grid3D.GridCell[,] gridCells;

        public Vector2Int GridSize => gridSize;
        public List<ElementData> ElementPool => elementPool;

        private bool UseProcedural => levelCreationMode == Grid3D.LevelCreationMode.Procedural;
        private bool IsTimerLimit => levelLimitType == LevelLimitType.Timer;
        private bool IsMovesLimit => levelLimitType == LevelLimitType.Moves;

        private static bool IsCauldronElementData(ElementData data)
        {
            if (data == null)
                return false;

            return GameManager.Instance != null && GameManager.Instance.cauldronElementData == data;
        }

        private static ElementPowerUpType ResolveElementPowerUpType(ElementData data)
        {
            return IsCauldronElementData(data) ? ElementPowerUpType.Cauldron : ElementPowerUpType.None;
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

        private static GridElementInfo CreateRandomElementInfo()
        {
            return new GridElementInfo
            {
                randomElement = true,
                powerUpType = ElementPowerUpType.None,
                cauldronProgress = 0
            };
        }
        private ElementData ResolveRandomRuntimeElement(Grid3D.GridCell[,] runtimeCells, int x, int y)
        {
            if (elementPool == null || elementPool.Count == 0)
                return null;

            List<ElementData> candidates = new List<ElementData>();
            for (int i = 0; i < elementPool.Count; i++)
            {
                ElementData candidate = elementPool[i];
                if (candidate == null)
                    continue;

                if (!WouldCreateInvalidStartupMatch(runtimeCells, x, y, candidate))
                    candidates.Add(candidate);
            }

            if (candidates.Count > 0)
                return candidates[Random.Range(0, candidates.Count)];

            List<ElementData> fallback = new List<ElementData>();
            for (int i = 0; i < elementPool.Count; i++)
            {
                if (elementPool[i] != null)
                    fallback.Add(elementPool[i]);
            }

            if (fallback.Count == 0)
                return null;

            return fallback[Random.Range(0, fallback.Count)];
        }

        private bool WouldCreateInvalidStartupMatch(Grid3D.GridCell[,] runtimeCells, int x, int y, ElementData data)
        {
            if (runtimeCells == null || data == null)
                return false;

            if (IsSameRuntimeElement(runtimeCells, x - 1, y, data) && IsSameRuntimeElement(runtimeCells, x - 2, y, data))
                return true;

            if (IsSameRuntimeElement(runtimeCells, x, y - 1, data) && IsSameRuntimeElement(runtimeCells, x, y - 2, data))
                return true;

            if (IsSameRuntimeElement(runtimeCells, x - 1, y, data) &&
                IsSameRuntimeElement(runtimeCells, x, y - 1, data) &&
                IsSameRuntimeElement(runtimeCells, x - 1, y - 1, data))
                return true;

            return false;
        }

        private bool IsSameRuntimeElement(Grid3D.GridCell[,] runtimeCells, int x, int y, ElementData data)
        {
            if (runtimeCells == null || data == null)
                return false;

            if (x < 0 || y < 0 || x >= runtimeCells.GetLength(0) || y >= runtimeCells.GetLength(1))
                return false;

            Grid3D.GridCell cell = runtimeCells[x, y];
            if (cell == null || cell.cellType != Grid3D.CellType.Normal || cell.elementInfo == null)
                return false;

            if (cell.elementInfo.isHidden || cell.elementInfo.powerUpType != ElementPowerUpType.None)
                return false;

            return cell.elementInfo.elementData == data;
        }

        private static Vector2Int GetGridCoverage(ElementData data)
        {
            if (data == null)
                return Vector2Int.one;

            return new Vector2Int(Mathf.Max(1, data.gridCoverage.x), Mathf.Max(1, data.gridCoverage.y));
        }

        private bool IsInsideGrid(Vector2Int pos)
        {
            return pos.x >= 0 && pos.y >= 0 && pos.x < gridSize.x && pos.y < gridSize.y;
        }

        private void NormalizeMultiCellCoverage()
        {
            EnsureGridCells();
            bool[,] occupied = new bool[gridSize.x, gridSize.y];

            for (int y = 0; y < gridSize.y; y++)
            {
                for (int x = 0; x < gridSize.x; x++)
                {
                    Grid3D.GridCell anchor = gridCells[x, y];
                    if (anchor == null)
                        continue;

                    if (anchor.cellType != Grid3D.CellType.Normal)
                    {
                        anchor.elementInfo = null;
                        continue;
                    }

                    if (anchor.elementInfo == null || anchor.elementInfo.elementData == null)
                        continue;

                    Vector2Int coverage = GetGridCoverage(anchor.elementInfo.elementData);
                    bool isValid = true;

                    for (int dx = 0; dx < coverage.x && isValid; dx++)
                    {
                        for (int dy = 0; dy < coverage.y; dy++)
                        {
                            Vector2Int pos = new Vector2Int(x + dx, y + dy);
                            if (!IsInsideGrid(pos))
                            {
                                isValid = false;
                                break;
                            }

                            Grid3D.GridCell targetCell = gridCells[pos.x, pos.y];
                            if (targetCell == null || targetCell.cellType != Grid3D.CellType.Normal || occupied[pos.x, pos.y])
                            {
                                isValid = false;
                                break;
                            }
                        }
                    }

                    if (!isValid)
                    {
                        anchor.elementInfo = null;
                        continue;
                    }

                    for (int dx = 0; dx < coverage.x; dx++)
                    {
                        for (int dy = 0; dy < coverage.y; dy++)
                        {
                            Vector2Int pos = new Vector2Int(x + dx, y + dy);
                            occupied[pos.x, pos.y] = true;
                            if (dx != 0 || dy != 0)
                                gridCells[pos.x, pos.y].elementInfo = null;
                        }
                    }
                }
            }
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
                        cellType = Grid3D.CellType.Normal,
                        elementInfo = CreateRandomElementInfo()
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
            NormalizeMultiCellCoverage();
            MarkDirty();
        }
#endif

#if UNITY_EDITOR
        private void MarkDirty()
        {
            EditorUtility.SetDirty(this);
        }
#else
        private void MarkDirty()
        {
        }
#endif

        public Grid3D.GridCell[,] CreateRuntimeGrid()
        {
            EnsureGridCells();
            NormalizeMultiCellCoverage();

            Grid3D.GridCell[,] runtimeCells = new Grid3D.GridCell[gridSize.x, gridSize.y];
            for (int x = 0; x < gridSize.x; x++)
            {
                for (int y = 0; y < gridSize.y; y++)
                {
                    Grid3D.GridCell sourceCell = gridCells[x, y];
                    Grid3D.CellType cellType = sourceCell != null ? sourceCell.cellType : Grid3D.CellType.Empty;
                    GridElementInfo sourceInfo = sourceCell != null ? sourceCell.elementInfo : null;
                    ElementData runtimeElementData = null;
                    bool hasSourceInfo = cellType == Grid3D.CellType.Normal && sourceInfo != null;
                    if (hasSourceInfo)
                    {
                        if (sourceInfo.randomElement)
                            runtimeElementData = ResolveRandomRuntimeElement(runtimeCells, x, y);
                        else
                            runtimeElementData = sourceInfo.elementData;
                    }

                    GridElementInfo elementInfo = cellType == Grid3D.CellType.Normal &&
                                                  sourceInfo != null &&
                                                  runtimeElementData != null
                        ? new GridElementInfo
                        {
                            elementData = runtimeElementData,
                            randomElement = false,
                            isSparkling = sourceInfo.isSparkling,
                            isHidden = sourceInfo.isHidden,
                            powerUpType = sourceInfo.powerUpType == ElementPowerUpType.None || sourceInfo.randomElement
                                ? ResolveElementPowerUpType(runtimeElementData)
                                : sourceInfo.powerUpType,
                            cauldronProgress = sourceInfo.cauldronProgress
                        }
                        : null;

                    runtimeCells[x, y] = new Grid3D.GridCell
                    {
                        coordinates = new Vector2Int(x, y),
                        cellType = cellType,
                        elementInfo = elementInfo,
                        cellFeature = sourceCell != null ? sourceCell.cellFeature : null,
                        breakableWallElementCondition = sourceCell != null ? sourceCell.breakableWallElementCondition : null,
                        cellFeatureGroupIndex = sourceCell != null ? sourceCell.cellFeatureGroupIndex : 0,
                        cellFeatureGroupHealth = sourceCell != null ? sourceCell.cellFeatureGroupHealth : 0,
                        cellFeatureGroupMaxHealth = sourceCell != null ? sourceCell.cellFeatureGroupMaxHealth : 0,
                        cellHealth = sourceCell != null && sourceCell.cellType == Grid3D.CellType.BreakableWall
                            ? Mathf.Max(1, sourceCell.cellHealth)
                            : 0
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

            // INITIALIZATION
            // Colors
            Color normalCellColor = new Color(219/255f, 186/255f, 143/255f);
            Color emptyCellColor = new Color(67/255f, 67/255f, 67/255f);
            Color breakableWallCellColor = new Color(140/255f, 100/255f, 86/255f);
            Color unbreakableWallCellColor = new Color(65/255f, 55/255f, 55/255f);
            // Rects
            Rect elementRect = new Rect(rect.x + rect.width * 0.1f, rect.y + rect.height * 0.1f, rect.width * 0.8f, rect.height * 0.8f);
            Rect featureRect = rect;
            Rect breakableWallElementRect = new Rect(rect.x + rect.width * 0.5f, rect.y, rect.width * 0.5f, rect.height * 0.5f);
            // Label Texts
            GUIStyle healthText = new GUIStyle()
            {
                alignment = TextAnchor.LowerLeft,
            };
            GUIStyle groupText = new GUIStyle()
            {
                alignment = TextAnchor.UpperRight,
            };

            // DRAWING
            // Draw cell background based on type
            DrawCellType(rect, breakableWallElementRect, value, normalCellColor, emptyCellColor, breakableWallCellColor, unbreakableWallCellColor);
            // Draw element sprite to elementRect if cell type is Normal and has elementInfo
            // Draw cell feature indicators in featureRect (e.g. icons or overlays for wafer, glass, electric field)
            DrawFeatures(value, featureRect);
            DrawElement(value, elementRect);
            // Draw health text for breakable walls and group index/health for glass features
            if (value.cellType == Grid3D.CellType.BreakableWall)
            {
                healthText.normal.textColor = Color.red;
                GUI.Label(rect, value.cellHealth.ToString(), healthText);
            }
            if(value.cellFeature is GlassFeature)
            {
                groupText.normal.textColor = Color.cyan;
                GUI.Label(rect, $"{value.cellFeatureGroupIndex}", groupText);
                healthText.normal.textColor = Color.blue;
                GUI.Label(rect, $"{value.cellFeatureGroupHealth}", healthText);
            }

            // EVENTS
            if (rect.Contains(Event.current.mousePosition))
            {
                // Handle right-click to open context menu for cell features and element assignment
                if (Event.current.type == EventType.MouseDown)
                {
                    // Right-click opens context menu
                    HandleCellRightClick(value);
                }
                // Handle shortcuts
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
                        value.cellHealth = Mathf.Max(1, value.cellHealth);
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
                    else if (Event.current.keyCode == KeyCode.H)
                    {
                        if (value.elementInfo != null)
                        {
                            value.elementInfo.isHidden = !value.elementInfo.isHidden;
                            MarkDirty();
                            Event.current.Use();
                        }
                    }
                    else if (Event.current.keyCode == KeyCode.G)
                    {
                        CreateElementInfo(GameManager.Instance.garbageBagElementData);
                        MarkDirty();
                        Event.current.Use();
                    }
                    else if (Event.current.keyCode == KeyCode.R)
                    {
                        if (value.cellType != Grid3D.CellType.Normal) value.cellType = Grid3D.CellType.Normal;
                        if (value.elementInfo != null)
                        {
                            value.elementInfo.elementData = null;
                            value.elementInfo.randomElement = !value.elementInfo.randomElement;
                        }
                        else
                        {
                            value.elementInfo = CreateRandomElementInfo();
                        }
                        MarkDirty();
                        Event.current.Use();
                    }
                    // Alphanumeric keys for glass group index and health (while hovering a glass cell)
                    else if (TryGetNumericShortcutValue(Event.current.keyCode, out int numericValue))
                    {
                        if (value.cellFeature is GlassFeature)
                        {
                            if (Event.current.shift)
                            {
                                value.cellFeatureGroupHealth = numericValue;
                            }
                            else
                            {
                                value.cellFeatureGroupIndex = numericValue;
                            }
                            MarkDirty();
                            Event.current.Use();
                        }
                        else if (value.cellType == Grid3D.CellType.BreakableWall)
                        {
                            {
                                value.cellHealth = Mathf.Max(1, numericValue);
                                MarkDirty();
                                Event.current.Use();
                            }
                        }
                    }
                    // Feature Placement
                    else if (Event.current.shift)
                    {
                        if (Event.current.keyCode == KeyCode.Z)
                        {
                            if (value.cellFeature is WaferFeature)
                                value.cellFeature = null;
                            else
                                value.cellFeature = GameManager.Instance.waferFeature;
                            MarkDirty();
                            Event.current.Use();
                        }
                        else if (Event.current.keyCode == KeyCode.X)
                        {
                            if (value.cellFeature is GlassFeature)
                                value.cellFeature = null;
                            else
                                value.cellFeature = GameManager.Instance.glassFeature;
                            MarkDirty();
                            Event.current.Use();
                        }
                        else if (Event.current.keyCode == KeyCode.A)
                        {
                            if (value.cellFeature is ElectricField)
                                value.cellFeature = null;
                            else
                                value.cellFeature = GameManager.Instance.electricField;
                            MarkDirty();
                            Event.current.Use();
                        }
                        else if (Event.current.keyCode == KeyCode.S)
                        {
                            if (value.cellFeature is LockedAreaFeature)
                                value.cellFeature = null;
                            else
                                value.cellFeature = GameManager.Instance.lockedAreaFeature;
                            MarkDirty();
                            Event.current.Use();
                        }
                    }
                }
            }

            return value;
        }

        private void HandleCellRightClick(Grid3D.GridCell value)
        {
            if (Event.current.button == 1)
            {
                GenericMenu menu = new GenericMenu();

                // Cell Type Options
                menu.AddItem(new GUIContent("Set Cell Type/Empty"), value.cellType == Grid3D.CellType.Empty, () =>
                {
                    value.cellType = Grid3D.CellType.Empty;
                    value.elementInfo = null;
                    MarkDirty();
                });
                menu.AddItem(new GUIContent("Set Cell Type/Normal"), value.cellType == Grid3D.CellType.Normal, () =>
                {
                    value.cellType = Grid3D.CellType.Normal;
                    MarkDirty();
                });
                menu.AddItem(new GUIContent("Set Cell Type/Breakable Wall"), value.cellType == Grid3D.CellType.BreakableWall, () =>
                {
                    value.cellType = Grid3D.CellType.BreakableWall;
                    value.elementInfo = null;
                    value.cellHealth = Mathf.Max(1, value.cellHealth);
                    MarkDirty();
                });
                menu.AddItem(new GUIContent("Set Cell Type/Unbreakable Wall"), value.cellType == Grid3D.CellType.UnbreakableWall, () =>
                {
                    value.cellType = Grid3D.CellType.UnbreakableWall;
                    value.elementInfo = null;
                    MarkDirty();
                });
                // Breakable Walls optios
                if(value.cellType == Grid3D.CellType.BreakableWall)
                {
                    menu.AddSeparator("");
                    // Element condition options.
                    menu.AddItem(new GUIContent("Breakable Walls/None"), value.breakableWallElementCondition == null, () =>
                    {
                        value.breakableWallElementCondition = null;
                        MarkDirty();
                    });
                    // Pool Elements
                    foreach (ElementData element in elementPool)
                    {
                        if (element != null)
                        {
                            menu.AddItem(new GUIContent($"Breakable Walls/Element Match Condition/{element.displayName}"), value.breakableWallElementCondition != null && value.breakableWallElementCondition == element, () =>
                            {
                                value.breakableWallElementCondition = element;
                                MarkDirty();
                            });
                        }
                    }
                    // Health options
                    for (int health = 1; health <= 3; health++)
                    {
                        int capturedHealth = health; // Capture loop variable
                        menu.AddItem(new GUIContent($"Breakable Walls/Health/{health}"), value.breakableWallElementCondition != null && value.cellHealth == capturedHealth, () =>
                        {
                            value.cellHealth = capturedHealth;
                            MarkDirty();
                        });
                    }
                }

                // Element Options (only for Normal cells)
                if (value.cellType == Grid3D.CellType.Normal)
                {
                    menu.AddItem(new GUIContent("Element/None"), value.elementInfo == null, () =>
                    {
                        value.elementInfo = null;
                        MarkDirty();
                    });
                    // Pool Elements
                    foreach (ElementData element in elementPool)
                    {
                        if (element != null)
                        {
                            menu.AddItem(new GUIContent($"Element/{element.displayName}"), value.elementInfo != null && value.elementInfo.elementData == element, () =>
                            {
                                value.elementInfo = CreateElementInfo(element);
                                MarkDirty();
                            });
                        }
                    }
                    // Power-Up Options (Rocket, Bomb, Propeller, Disco Ball)
                    ElementData rocketData = GameManager.Instance.rocketElementData;
                    ElementData bombData = GameManager.Instance.bombElementData;
                    ElementData propellerData = GameManager.Instance.propellerElementData;
                    ElementData discoBallData = GameManager.Instance.discoBallElementData;
                    menu.AddItem(new GUIContent($"Element/Power-Ups/{rocketData.displayName}"), value.elementInfo != null && value.elementInfo.elementData == rocketData, () =>
                    {
                        value.elementInfo = CreateElementInfo(rocketData);
                        MarkDirty();
                    });
                    menu.AddItem(new GUIContent($"Element/Power-Ups/{bombData.displayName}"), value.elementInfo != null && value.elementInfo.elementData == bombData, () =>
                    {
                        value.elementInfo = CreateElementInfo(bombData);
                        MarkDirty();
                    });
                    menu.AddItem(new GUIContent($"Element/Power-Ups/{propellerData.displayName}"), value.elementInfo != null && value.elementInfo.elementData == propellerData, () =>
                    {
                        value.elementInfo = CreateElementInfo(propellerData);
                        MarkDirty();
                    });
                    menu.AddItem(new GUIContent($"Element/Power-Ups/{discoBallData.displayName}"), value.elementInfo != null && value.elementInfo.elementData == discoBallData, () =>
                    {
                        value.elementInfo = CreateElementInfo(discoBallData);
                        MarkDirty();
                    });
                    // Special Elements (Cauldron, Power Generator, Power Outlet, etc.)
                    ElementData cauldronData = GameManager.Instance.cauldronElementData;
                    ElementData powerGeneratorData = GameManager.Instance.powerGeneratorElementData;
                    ElementData powerOutletData = GameManager.Instance.powerOutletElementData;
                    ElementData garbageBagData = GameManager.Instance.garbageBagElementData;
                    menu.AddItem(new GUIContent($"Element/Special/{cauldronData.displayName}"), value.elementInfo != null && value.elementInfo.elementData == cauldronData, () =>
                    {
                        value.elementInfo = CreateElementInfo(cauldronData);
                        MarkDirty();
                    });
                    menu.AddItem(new GUIContent($"Element/Special/{powerGeneratorData.displayName}"), value.elementInfo != null && value.elementInfo.elementData == powerGeneratorData, () =>
                    {
                        value.elementInfo = CreateElementInfo(powerGeneratorData);
                        MarkDirty();
                    });
                    menu.AddItem(new GUIContent($"Element/Special/{powerOutletData.displayName}"), value.elementInfo != null && value.elementInfo.elementData == powerOutletData, () =>
                    {
                        value.elementInfo = CreateElementInfo(powerOutletData);
                        MarkDirty();
                    });
                    menu.AddItem(new GUIContent($"Element/Special/{garbageBagData.displayName}"), value.elementInfo != null && value.elementInfo.elementData == garbageBagData, () =>
                    {
                        value.elementInfo = CreateElementInfo(garbageBagData);
                        MarkDirty();
                    });
                }

                // Feature Options
                menu.AddItem(new GUIContent("Toggle Feature/Wafer"), value.cellFeature is WaferFeature, () =>
                {
                    if (value.cellFeature is WaferFeature)
                        value.cellFeature = null;
                    else
                        value.cellFeature = GameManager.Instance.waferFeature;
                    MarkDirty();
                });
                menu.AddItem(new GUIContent("Toggle Feature/Glass"), value.cellFeature is GlassFeature, () =>
                {
                    if (value.cellFeature is GlassFeature)
                        value.cellFeature = null;
                    else
                        value.cellFeature = GameManager.Instance.glassFeature;
                    MarkDirty();
                });
                menu.AddItem(new GUIContent("Toggle Feature/Electric Field"), value.cellFeature is ElectricField, () =>
                {
                    if (value.cellFeature is ElectricField)
                        value.cellFeature = null;
                    else
                        value.cellFeature = GameManager.Instance.electricField;
                    MarkDirty();
                });
                menu.AddItem(new GUIContent("Toggle Feature/Locked Area"), value.cellFeature is LockedAreaFeature, () =>
                {
                    if (value.cellFeature is LockedAreaFeature)
                        value.cellFeature = null;
                    else
                        value.cellFeature = GameManager.Instance.lockedAreaFeature;
                    MarkDirty();
                });
                menu.ShowAsContext();
                Event.current.Use();
            }
        }

        private static void DrawFeatures(Grid3D.GridCell value, Rect featureRect)
        {
            if (value.cellFeature != null)
            {
                if (value.cellFeature is WaferFeature wafer)
                {
                    DrawSprite(featureRect, wafer.tileSpriteSet.singleInnerTile);
                }
                if (value.cellFeature is GlassFeature glass)
                {
                    DrawSprite(featureRect, glass.tileSpriteSet.singleInnerTile);
                }
                if (value.cellFeature is ElectricField electricField)
                {
                    DrawSprite(featureRect, electricField.tileSpriteSet.singleInnerTile);
                }
                // Note: Add indicators for the future cell features here
                // ...
            }
        }
        private static void DrawElement(Grid3D.GridCell value, Rect elementRect)
        {
            if (value.cellType == Grid3D.CellType.Normal && value.elementInfo != null)
            {
                if(value.elementInfo.elementData != null)
                {
                    Sprite elementSprite = value.elementInfo.elementData.displayIcon;
                    if (elementSprite != null)
                    {
                        DrawSprite(elementRect, elementSprite);
                    }
                }
                else if (value.elementInfo.randomElement)
                {
                    GUIStyle randomStyle = new GUIStyle();
                    randomStyle.fontStyle = FontStyle.Bold;
                    randomStyle.fontSize = 36;
                    randomStyle.alignment = TextAnchor.MiddleCenter;
                    GUI.Label(elementRect, "?", randomStyle);
                }

                if (value.elementInfo.isHidden)
                {
                    Rect cornerRect = new Rect(elementRect.xMax - elementRect.width * 0.3f, elementRect.yMax - elementRect.height * 0.3f, elementRect.width * 0.3f, elementRect.height * 0.3f);
                    DrawSprite(cornerRect, GameManager.Instance.gfxManager.hiddenIndicatorIcon);
                }
            }
        }

        private static void DrawSprite(Rect drawRect, Sprite sprite)
        {
            if (sprite == null || sprite.texture == null)
                return;

            Texture2D texture = sprite.texture;
            Rect spriteRect = sprite.textureRect;
            Rect uv = new Rect(
                spriteRect.x / texture.width,
                spriteRect.y / texture.height,
                spriteRect.width / texture.width,
                spriteRect.height / texture.height);

            GUI.DrawTextureWithTexCoords(drawRect, texture, uv, true);
        }

        private static void DrawCellType(Rect rect, Rect breakableWallElementIndicatorRect, Grid3D.GridCell value, Color normalCellColor, Color emptyCellColor, Color breakableWallCellColor, Color unbreakableWallCellColor)
        {
            switch (value.cellType)
            {
                case Grid3D.CellType.Normal:
                    EditorGUI.DrawRect(rect, normalCellColor);
                    break;
                case Grid3D.CellType.Empty:
                    EditorGUI.DrawRect(rect, emptyCellColor);
                    break;
                case Grid3D.CellType.BreakableWall:
                    EditorGUI.DrawRect(rect, breakableWallCellColor);
                    if (value.breakableWallElementCondition != null)
                        DrawSprite(breakableWallElementIndicatorRect, value.breakableWallElementCondition.displayIcon);
                    break;
                case Grid3D.CellType.UnbreakableWall:
                    EditorGUI.DrawRect(rect, unbreakableWallCellColor);
                    break;
            }
        }
#endif
    }
}
