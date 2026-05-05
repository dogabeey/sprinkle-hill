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
            "R: Toggle random element marker\n" +
            "Ctrl+R: Assign random element from pool\n" +
            "1-9: Assign element by indexed asset list\n" +
            "Right Click: Open element/cell feature menu";

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

        private static bool IsGarbageBagElementData(ElementData data)
        {
            return data != null && GameManager.Instance != null && GameManager.Instance.garbageBagElementData == data;
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

        private ElementData GetRandomElementFromPool()
        {
            if (elementPool == null || elementPool.Count == 0)
                return null;

            return elementPool[Random.Range(0, elementPool.Count)];
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

        private void ToggleRandomElementAt(Vector2Int position)
        {
            if (!IsInsideGrid(position))
                return;

            EnsureGridCells();
            ClearElementsIntersectingArea(position, Vector2Int.one);

            Grid3D.GridCell targetCell = gridCells[position.x, position.y];
            targetCell.cellType = Grid3D.CellType.Normal;

            if (targetCell.elementInfo != null && targetCell.elementInfo.randomElement)
            {
                targetCell.elementInfo = null;
                return;
            }

            targetCell.elementInfo = CreateRandomElementInfo();
        }

        private bool PlaceRandomElementFromPoolAt(Vector2Int position)
        {
            ElementData randomElement = GetRandomElementFromPool();
            if (randomElement == null)
                return false;

            PlaceElementAt(position, randomElement);
            return true;
        }

        private void ApplyElementSelectionToCell(Grid3D.GridCell cell, ElementData selectedElement)
        {
            if (cell == null)
                return;

            if (cell.cellType == Grid3D.CellType.BreakableWall)
            {
                cell.breakableWallElementCondition = selectedElement;
                MarkDirty();
                return;
            }

            if (selectedElement == null)
                return;

            if (TryGetCellCoordinates(cell, out Vector2Int cellPos))
                PlaceElementAt(cellPos, selectedElement);
            else
            {
                cell.cellType = Grid3D.CellType.Normal;
                cell.elementInfo = CreateElementInfo(selectedElement);
            }

            MarkDirty();
        }

 #if UNITY_EDITOR
        private void ShowElementSelectionMenu(Grid3D.GridCell cell, bool includeCellFeatureItems)
        {
            GenericMenu menu = new GenericMenu();
            bool isBreakableWall = cell != null && cell.cellType == Grid3D.CellType.BreakableWall;
            WaferFeature waferFeature = GameManager.Instance != null ? GameManager.Instance.waferFeature : null;
            GlassFeature glassFeature = GameManager.Instance != null ? GameManager.Instance.glassFeature : null;

            if (includeCellFeatureItems)
            {
                menu.AddItem(new GUIContent("Cell Feature/None"), cell.cellFeature == null, () =>
                {
                    cell.cellFeature = null;
                    MarkDirty();
                });
                if (waferFeature != null)
                {
                    menu.AddItem(new GUIContent($"Cell Feature/{waferFeature.name}"), cell.cellFeature == waferFeature, () =>
                    {
                        cell.cellType = Grid3D.CellType.Normal;
                        cell.cellFeature = waferFeature;
                        cell.breakableWallElementCondition = null;
                        MarkDirty();
                    });
                }
                if (glassFeature != null)
                {
                    menu.AddItem(new GUIContent($"Cell Feature/{glassFeature.name}"), cell.cellFeature == glassFeature, () =>
                    {
                        cell.cellType = Grid3D.CellType.Normal;
                        cell.cellFeature = glassFeature;
                        cell.breakableWallElementCondition = null;
                        MarkDirty();
                    });
                }

                menu.AddSeparator("");
            }

            if (isBreakableWall)
            {
                menu.AddItem(new GUIContent("Break Condition/Clear"), cell.breakableWallElementCondition == null, () =>
                {
                    cell.breakableWallElementCondition = null;
                    MarkDirty();
                });

                HashSet<ElementData> addedElements = new HashSet<ElementData>();
                if (elementPool != null)
                {
                    for (int i = 0; i < elementPool.Count; i++)
                    {
                        ElementData pooledElement = elementPool[i];
                        if (pooledElement == null || !addedElements.Add(pooledElement))
                            continue;

                        ElementData capturedPooledElement = pooledElement;
                        bool isSelected = cell.breakableWallElementCondition == capturedPooledElement;
                        menu.AddItem(new GUIContent($"Break Condition/{capturedPooledElement.name}"), isSelected, () =>
                        {
                            cell.breakableWallElementCondition = capturedPooledElement;
                            MarkDirty();
                        });
                    }
                }

                menu.ShowAsContext();
                return;
            }

            if (elementPool != null && elementPool.Count > 0)
            {
                menu.AddItem(new GUIContent("Random Element (Pool)"), false, () =>
                {
                    ElementData randomElement = elementPool[Random.Range(0, elementPool.Count)];
                    ApplyElementSelectionToCell(cell, randomElement);
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
                menu.AddItem(new GUIContent($"{capturedPooledElement.name} {capturedPooledElement.gridCoverage.x}x{capturedPooledElement.gridCoverage.y}"), false, () =>
                {
                    ApplyElementSelectionToCell(cell, capturedPooledElement);
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
                    string category = GetCategoryBasedOnElementType(capturedElementData);
                    menu.AddItem(new GUIContent(category + elementData.name), false, () =>
                    {
                        ApplyElementSelectionToCell(cell, capturedElementData);
                    });
                }
            }

            menu.ShowAsContext();
        }
 #endif

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

        private static bool AreasOverlap(Vector2Int aStart, Vector2Int aSize, Vector2Int bStart, Vector2Int bSize)
        {
            int aMinX = aStart.x;
            int aMaxX = aStart.x + aSize.x - 1;
            int aMinY = aStart.y;
            int aMaxY = aStart.y + aSize.y - 1;

            int bMinX = bStart.x;
            int bMaxX = bStart.x + bSize.x - 1;
            int bMinY = bStart.y;
            int bMaxY = bStart.y + bSize.y - 1;

            return aMinX <= bMaxX && aMaxX >= bMinX && aMinY <= bMaxY && aMaxY >= bMinY;
        }

        private bool TryGetCellCoordinates(Grid3D.GridCell cell, out Vector2Int coordinates)
        {
            EnsureGridCells();
            for (int x = 0; x < gridSize.x; x++)
            {
                for (int y = 0; y < gridSize.y; y++)
                {
                    if (ReferenceEquals(gridCells[x, y], cell))
                    {
                        coordinates = new Vector2Int(x, y);
                        return true;
                    }
                }
            }

            coordinates = default;
            return false;
        }

        private void ClearElementsIntersectingArea(Vector2Int start, Vector2Int size)
        {
            EnsureGridCells();

            for (int x = 0; x < gridSize.x; x++)
            {
                for (int y = 0; y < gridSize.y; y++)
                {
                    Grid3D.GridCell cell = gridCells[x, y];
                    if (cell == null || cell.elementInfo == null || cell.elementInfo.elementData == null)
                        continue;

                    Vector2Int otherStart = new Vector2Int(x, y);
                    Vector2Int otherSize = GetGridCoverage(cell.elementInfo.elementData);
                    if (!AreasOverlap(start, size, otherStart, otherSize))
                        continue;

                    cell.elementInfo = null;
                }
            }
        }

        private void PlaceElementAt(Vector2Int start, ElementData data)
        {
            if (data == null || !IsInsideGrid(start))
                return;

            Vector2Int size = GetGridCoverage(data);
            ClearElementsIntersectingArea(start, size);

            Grid3D.GridCell anchorCell = gridCells[start.x, start.y];
            anchorCell.cellType = Grid3D.CellType.Normal;
            anchorCell.elementInfo = CreateElementInfo(data);

            for (int dx = 0; dx < size.x; dx++)
            {
                for (int dy = 0; dy < size.y; dy++)
                {
                    Vector2Int pos = new Vector2Int(start.x + dx, start.y + dy);
                    if (!IsInsideGrid(pos) || pos == start)
                        continue;

                    Grid3D.GridCell coveredCell = gridCells[pos.x, pos.y];
                    if (coveredCell != null)
                        coveredCell.elementInfo = null;
                }
            }
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
        private void ShowElementInfoMenu(Grid3D.GridCell cell)
        {
            if (cell == null || cell.elementInfo == null)
                return;

            GridElementInfo info = cell.elementInfo;
            GenericMenu menu = new GenericMenu();

            menu.AddDisabledItem(new GUIContent("Element Info"));
            menu.AddDisabledItem(new GUIContent($"Element Data: {(info.elementData != null ? info.elementData.name : "<none>")}"));
            menu.AddSeparator("");

            menu.AddItem(new GUIContent("Random Element"), info.randomElement, () =>
            {
                info.randomElement = !info.randomElement;
                if (info.randomElement)
                {
                    info.powerUpType = ElementPowerUpType.None;
                    info.cauldronProgress = 0;
                }
                MarkDirty();
            });

            menu.AddItem(new GUIContent("Sparkling"), info.isSparkling, () =>
            {
                info.isSparkling = !info.isSparkling;
                MarkDirty();
            });

            menu.AddItem(new GUIContent("Hidden"), info.isHidden, () =>
            {
                info.isHidden = !info.isHidden;
                MarkDirty();
            });

            menu.ShowAsContext();
        }

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
                        breakableWallElementCondition = sourceCell != null ? sourceCell.breakableWallElementCondition : null
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
            float indicatorSize = Mathf.Min(rect.width, rect.height) * 0.26f;
            float indicatorPadding = Mathf.Min(rect.width, rect.height) * 0.05f;
            Rect hiddenIndicatorRect = new Rect(rect.x + indicatorPadding, rect.y + indicatorPadding, indicatorSize, indicatorSize);
            Rect sparklingIndicatorRect = new Rect(hiddenIndicatorRect.xMax + indicatorPadding * 0.6f, rect.y + indicatorPadding, indicatorSize, indicatorSize);

            WaferFeature waferFeature = GameManager.Instance.waferFeature;
            GlassFeature glassFeature = GameManager.Instance != null ? GameManager.Instance.glassFeature : null;

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
                value.cellFeature = null;
            }

            if (value.cellType == Grid3D.CellType.BreakableWall && value.breakableWallElementCondition != null)
            {
                GUIStyle conditionStyle = new GUIStyle(EditorStyles.boldLabel)
                {
                    alignment = TextAnchor.LowerCenter,
                    fontSize = Mathf.Max(8, Mathf.RoundToInt(rect.height * 0.16f))
                };
                conditionStyle.normal.textColor = Color.white;
                EditorGUI.LabelField(new Rect(rect.x + 1f, rect.yMax - indicatorSize - 1f, rect.width - 2f, indicatorSize), $"[{value.breakableWallElementCondition.name}]", conditionStyle);
            }
            if (value.cellType == Grid3D.CellType.Normal && value.cellFeature != null)
            {
                Sprite featureIcon = value.cellFeature.featureIcon;
                if (featureIcon != null && featureIcon.texture != null)
                {
                    Rect spriteRect = featureIcon.textureRect;
                    Rect uv = new Rect(
                        spriteRect.x / featureIcon.texture.width,
                        spriteRect.y / featureIcon.texture.height,
                        spriteRect.width / featureIcon.texture.width,
                        spriteRect.height / featureIcon.texture.height);
                    GUI.DrawTextureWithTexCoords(rect, featureIcon.texture, uv, true);
                }
                else
                {
                    GUI.DrawTexture(rect, Texture2D.whiteTexture);
                }
            }

            if (value.elementInfo != null && value.elementInfo.randomElement)
            {
                GUIStyle questionStyle = new GUIStyle(EditorStyles.boldLabel)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = Mathf.Max(12, Mathf.RoundToInt(rect.height * 0.8f))
                };
                questionStyle.normal.textColor = Color.white;
                EditorGUI.LabelField(rect, "?", questionStyle);
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

            if (value.cellType == Grid3D.CellType.Normal && value.elementInfo != null)
            {
                Gfx gfx = GameManager.Instance != null ? GameManager.Instance.gfxManager : null;

                if (value.elementInfo.isHidden)
                {
                    Sprite hiddenIcon = gfx != null ? gfx.hiddenIndicatorIcon : null;
                    if (hiddenIcon != null && hiddenIcon.texture != null)
                    {
                        Rect hiddenSpriteRect = hiddenIcon.textureRect;
                        Rect hiddenUv = new Rect(
                            hiddenSpriteRect.x / hiddenIcon.texture.width,
                            hiddenSpriteRect.y / hiddenIcon.texture.height,
                            hiddenSpriteRect.width / hiddenIcon.texture.width,
                            hiddenSpriteRect.height / hiddenIcon.texture.height);
                        GUI.DrawTextureWithTexCoords(hiddenIndicatorRect, hiddenIcon.texture, hiddenUv, true);
                    }
                    else
                    {
                        EditorGUI.LabelField(hiddenIndicatorRect, "H", EditorStyles.whiteMiniLabel);
                    }
                }

                if (value.elementInfo.isSparkling)
                {
                    Sprite sparklingIcon = gfx != null ? gfx.sparklingIndicatorIcon : null;
                    if (sparklingIcon != null && sparklingIcon.texture != null)
                    {
                        Rect sparklingSpriteRect = sparklingIcon.textureRect;
                        Rect sparklingUv = new Rect(
                            sparklingSpriteRect.x / sparklingIcon.texture.width,
                            sparklingSpriteRect.y / sparklingIcon.texture.height,
                            sparklingSpriteRect.width / sparklingIcon.texture.width,
                            sparklingSpriteRect.height / sparklingIcon.texture.height);
                        GUI.DrawTextureWithTexCoords(sparklingIndicatorRect, sparklingIcon.texture, sparklingUv, true);
                    }
                    else
                    {
                        EditorGUI.LabelField(sparklingIndicatorRect, "S", EditorStyles.whiteMiniLabel);
                    }
                }
            }

            if (rect.Contains(Event.current.mousePosition))
            {
                if (Event.current.type == EventType.KeyDown)
                {
                    if (Event.current.shift)
                    {
                        if(Event.current.keyCode == KeyCode.Z)
                            {
                            value.cellType = Grid3D.CellType.Normal;
                            value.cellFeature = value.cellFeature == waferFeature ? null : waferFeature;
                            MarkDirty();
                        }
                        else if (Event.current.keyCode == KeyCode.X)
                        {
                            value.cellType = Grid3D.CellType.Normal;
                            value.cellFeature = value.cellFeature == glassFeature ? null : glassFeature;
                            MarkDirty();
                        }
                        Event.current.Use();
                    }
                    if (Event.current.keyCode == KeyCode.E)
                    {
                        if (TryGetCellCoordinates(value, out Vector2Int cellPos))
                            ClearElementsIntersectingArea(cellPos, Vector2Int.one);
                        value.cellType = Grid3D.CellType.Empty;
                        value.elementInfo = null;
                        value.cellFeature = null;
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
                        if (TryGetCellCoordinates(value, out Vector2Int cellPos))
                            ClearElementsIntersectingArea(cellPos, Vector2Int.one);
                        value.cellType = Grid3D.CellType.BreakableWall;
                        value.breakableWallElementCondition = null;
                        value.elementInfo = null;
                        value.cellFeature = null;
                        MarkDirty();
                        Event.current.Use();
                    }
                    else if (Event.current.keyCode == KeyCode.U)
                    {
                        if (TryGetCellCoordinates(value, out Vector2Int cellPos))
                            ClearElementsIntersectingArea(cellPos, Vector2Int.one);
                        value.cellType = Grid3D.CellType.UnbreakableWall;
                        value.breakableWallElementCondition = null;
                        value.elementInfo = null;
                        value.cellFeature = null;
                        MarkDirty();
                        Event.current.Use();
                    }
                    else if (Event.current.keyCode == KeyCode.H)
                    {
                        value.elementInfo.isHidden = !value.elementInfo.isHidden;
                        MarkDirty();
                        Event.current.Use();
                    }
                    else if (Event.current.keyCode == KeyCode.R)
                    {
                        bool usePoolRandom = Event.current.control || Event.current.command;
                        if (usePoolRandom)
                        {
                            if (TryGetCellCoordinates(value, out Vector2Int cellPos))
                            {
                                if (PlaceRandomElementFromPoolAt(cellPos))
                                    MarkDirty();
                            }
                            else
                            {
                                ElementData randomElement = GetRandomElementFromPool();
                                if (randomElement != null)
                                {
                                    value.cellType = Grid3D.CellType.Normal;
                                    value.elementInfo = CreateElementInfo(randomElement);
                                    MarkDirty();
                                }
                            }
                        }
                        else
                        {
                            if (TryGetCellCoordinates(value, out Vector2Int cellPos))
                                ToggleRandomElementAt(cellPos);
                            else
                            {
                                value.cellType = Grid3D.CellType.Normal;
                                if (value.elementInfo != null && value.elementInfo.randomElement)
                                    value.elementInfo = null;
                                else
                                    value.elementInfo = CreateRandomElementInfo();
                            }
                            MarkDirty();
                        }

                        Event.current.Use();
                    }
                }
                else if (Event.current.type == EventType.MouseDown && Event.current.button == 0)
                {
                    if (value.cellType == Grid3D.CellType.Normal && value.elementInfo != null)
                    {
                        ShowElementInfoMenu(value);
                        Event.current.Use();
                    }
                    else if (value.cellType == Grid3D.CellType.BreakableWall)
                    {
                        ShowElementSelectionMenu(value, false);
                        Event.current.Use();
                    }
                }
                else if ((Event.current.type == EventType.MouseDown && Event.current.button == 1) || Event.current.type == EventType.ContextClick)
                {
                    ShowElementSelectionMenu(value, true);
                    Event.current.Use();
                }
            }

            return value;
        }
#endif
        private string GetCategoryBasedOnElementType(ElementData capturedElementData)
        {
            if (capturedElementData == null)
                return "Other Elements/";

            if (IsCauldronElementData(capturedElementData) || IsGarbageBagElementData(capturedElementData))
                return "Special Elements/";

            return "Other Elements/";
        }
    }
}
