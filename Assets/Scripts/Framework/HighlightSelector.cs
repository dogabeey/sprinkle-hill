using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Game
{
    public abstract class HighlightSelector
    {
        public abstract GameObject[] HighlightedObjects { get; }
    }

    public class TwoRandomMatchableElements_Highlight : HighlightSelector
    {
        private static readonly Vector2Int[] Dirs = { Vector2Int.right, Vector2Int.up };

        public override GameObject[] HighlightedObjects
        {
            get
            {
                Match3Grid grid = GetGrid();
                if (grid == null) return new GameObject[0];

                Vector2Int size = grid.GridSize;
                for (int x = 0; x < size.x; x++)
                {
                    for (int y = 0; y < size.y; y++)
                    {
                        Vector2Int posA = new Vector2Int(x, y);
                        foreach (Vector2Int dir in Dirs)
                        {
                            Vector2Int posB = posA + dir;
                            if (posB.x >= size.x || posB.y >= size.y) continue;

                            if (TryGetOrderedSwap(grid, posA, posB, out Vector2Int mover, out Vector2Int target))
                                return CollectGameObjects(grid, mover, target);
                        }
                    }
                }

                return new GameObject[0];
            }
        }

        private static Match3Grid GetGrid()
        {
            if (GameManager.Instance == null) return null;
            return GameManager.Instance.CurrentLevel is LevelScene_Match3Game lvl ? lvl.grid as Match3Grid : null;
        }

        private static bool TryGetOrderedSwap(Match3Grid grid, Vector2Int a, Vector2Int b,
                                              out Vector2Int mover, out Vector2Int target)
        {
            mover = a;
            target = b;

            Grid3D.GridCell cellA = grid.GetCellPublic(a);
            Grid3D.GridCell cellB = grid.GetCellPublic(b);

            if (!IsMatchable(cellA) || !IsMatchable(cellB)) return false;

            ElementData dataA = cellA.elementInfo.elementData;
            ElementData dataB = cellB.elementInfo.elementData;
            if (dataA == dataB) return false;

            cellA.elementInfo.elementData = dataB;
            cellB.elementInfo.elementData = dataA;

            bool aIsMover = CreatesLineMatchAt(grid, b);
            bool bIsMover = CreatesLineMatchAt(grid, a);

            cellA.elementInfo.elementData = dataA;
            cellB.elementInfo.elementData = dataB;

            if (aIsMover)
            {
                mover = a;
                target = b;
                return true;
            }

            if (bIsMover)
            {
                mover = b;
                target = a;
                return true;
            }

            return false;
        }

        private static bool CreatesLineMatchAt(Match3Grid grid, Vector2Int pos)
        {
            Grid3D.GridCell center = grid.GetCellPublic(pos);
            if (!IsMatchable(center)) return false;

            ElementData data = center.elementInfo.elementData;

            int h = 1 + CountDirection(grid, pos, Vector2Int.left, data)
                      + CountDirection(grid, pos, Vector2Int.right, data);
            if (h >= 3) return true;

            int v = 1 + CountDirection(grid, pos, Vector2Int.down, data)
                      + CountDirection(grid, pos, Vector2Int.up, data);
            return v >= 3;
        }

        private static int CountDirection(Match3Grid grid, Vector2Int origin, Vector2Int dir, ElementData data)
        {
            int count = 0;
            Vector2Int cur = origin + dir;

            while (true)
            {
                Grid3D.GridCell cell = grid.GetCellPublic(cur);
                if (!IsMatchable(cell) || cell.elementInfo.elementData != data) break;
                count++;
                cur += dir;
            }

            return count;
        }

        private static bool IsMatchable(Grid3D.GridCell cell)
        {
            return cell != null
                && cell.cellType == Grid3D.CellType.Normal
                && cell.elementInfo != null
                && !cell.elementInfo.isHidden
                && cell.elementInfo.powerUpType == ElementPowerUpType.None
                && cell.elementInfo.elementData != null;
        }

        private static GameObject[] CollectGameObjects(Match3Grid grid, Vector2Int a, Vector2Int b)
        {
            GridElement elA = grid.GetElementAt(a);
            GridElement elB = grid.GetElementAt(b);

            if (elA != null && elB != null)
                return new[] { elA.gameObject, elB.gameObject };
            if (elA != null)
                return new[] { elA.gameObject };
            if (elB != null)
                return new[] { elB.gameObject };

            return new GameObject[0];
        }
    }

    public class TwoRandomSquareMatchableElement_Highlight : HighlightSelector
    {
        private static readonly Vector2Int[] Dirs = { Vector2Int.right, Vector2Int.up };

        public override GameObject[] HighlightedObjects
        {
            get
            {
                Match3Grid grid = GetGrid();
                if (grid == null) return new GameObject[0];

                Vector2Int size = grid.GridSize;
                for (int x = 0; x < size.x; x++)
                {
                    for (int y = 0; y < size.y; y++)
                    {
                        Vector2Int posA = new Vector2Int(x, y);
                        foreach (Vector2Int dir in Dirs)
                        {
                            Vector2Int posB = posA + dir;
                            if (posB.x >= size.x || posB.y >= size.y) continue;

                            if (TryGetOrderedSquareSwap(grid, posA, posB, out Vector2Int mover, out Vector2Int target))
                                return CollectGameObjects(grid, mover, target);
                        }
                    }
                }

                return new GameObject[0];
            }
        }

        private static Match3Grid GetGrid()
        {
            if (GameManager.Instance == null) return null;
            return GameManager.Instance.CurrentLevel is LevelScene_Match3Game lvl ? lvl.grid as Match3Grid : null;
        }

        private static bool TryGetOrderedSquareSwap(Match3Grid grid, Vector2Int a, Vector2Int b,
                                                    out Vector2Int mover, out Vector2Int target)
        {
            mover = a;
            target = b;

            Grid3D.GridCell cellA = grid.GetCellPublic(a);
            Grid3D.GridCell cellB = grid.GetCellPublic(b);

            if (!IsMatchable(cellA) || !IsMatchable(cellB)) return false;

            ElementData dataA = cellA.elementInfo.elementData;
            ElementData dataB = cellB.elementInfo.elementData;
            if (dataA == dataB) return false;

            cellA.elementInfo.elementData = dataB;
            cellB.elementInfo.elementData = dataA;

            bool aIsMover = CreatesSquareAt(grid, b);
            bool bIsMover = CreatesSquareAt(grid, a);

            cellA.elementInfo.elementData = dataA;
            cellB.elementInfo.elementData = dataB;

            if (aIsMover)
            {
                mover = a;
                target = b;
                return true;
            }

            if (bIsMover)
            {
                mover = b;
                target = a;
                return true;
            }

            return false;
        }

        private static bool CreatesSquareAt(Match3Grid grid, Vector2Int pos)
        {
            Grid3D.GridCell center = grid.GetCellPublic(pos);
            if (!IsMatchable(center)) return false;

            ElementData data = center.elementInfo.elementData;
            Vector2Int[] origins =
            {
                pos,
                pos + Vector2Int.left,
                pos + Vector2Int.down,
                pos + Vector2Int.left + Vector2Int.down
            };

            for (int i = 0; i < origins.Length; i++)
            {
                if (IsSquareAt(grid, origins[i], data)) return true;
            }

            return false;
        }

        private static bool IsSquareAt(Match3Grid grid, Vector2Int origin, ElementData data)
        {
            Grid3D.GridCell c00 = grid.GetCellPublic(origin);
            Grid3D.GridCell c10 = grid.GetCellPublic(origin + Vector2Int.right);
            Grid3D.GridCell c01 = grid.GetCellPublic(origin + Vector2Int.up);
            Grid3D.GridCell c11 = grid.GetCellPublic(origin + Vector2Int.right + Vector2Int.up);

            return IsMatchable(c00) && c00.elementInfo.elementData == data
                && IsMatchable(c10) && c10.elementInfo.elementData == data
                && IsMatchable(c01) && c01.elementInfo.elementData == data
                && IsMatchable(c11) && c11.elementInfo.elementData == data;
        }

        private static bool IsMatchable(Grid3D.GridCell cell)
        {
            return cell != null
                && cell.cellType == Grid3D.CellType.Normal
                && cell.elementInfo != null
                && !cell.elementInfo.isHidden
                && cell.elementInfo.powerUpType == ElementPowerUpType.None
                && cell.elementInfo.elementData != null;
        }

        private static GameObject[] CollectGameObjects(Match3Grid grid, Vector2Int a, Vector2Int b)
        {
            GridElement elA = grid.GetElementAt(a);
            GridElement elB = grid.GetElementAt(b);

            if (elA != null && elB != null)
                return new[] { elA.gameObject, elB.gameObject };
            if (elA != null)
                return new[] { elA.gameObject };
            if (elB != null)
                return new[] { elB.gameObject };

            return new GameObject[0];
        }
    }

    public class Bomb_Highlight : HighlightSelector
    {
        public override GameObject[] HighlightedObjects => HighlightSelectorPowerUpUtility.GetRandomPowerUpObjects(ElementPowerUpType.Bomb, ElementPowerUpType.BigBomb);
    }

    public class Rocket_Highlight : HighlightSelector
    {
        public override GameObject[] HighlightedObjects => HighlightSelectorPowerUpUtility.GetRandomPowerUpObjects(ElementPowerUpType.VerticalRocket, ElementPowerUpType.HorizontalRocket);
    }

    public class DiscoBall_Highlight : HighlightSelector
    {
        public override GameObject[] HighlightedObjects => HighlightSelectorPowerUpUtility.GetRandomPowerUpObjects(ElementPowerUpType.DiscoBall);
    }

    internal static class HighlightSelectorPowerUpUtility
    {
        public static GameObject[] GetRandomPowerUpObjects(params ElementPowerUpType[] acceptedTypes)
        {
            Match3Grid grid = GetGrid();
            if (grid == null || acceptedTypes == null || acceptedTypes.Length == 0)
                return new GameObject[0];

            List<GameObject> candidates = new List<GameObject>();
            Vector2Int size = grid.GridSize;

            for (int x = 0; x < size.x; x++)
            {
                for (int y = 0; y < size.y; y++)
                {
                    Vector2Int pos = new Vector2Int(x, y);
                    Grid3D.GridCell cell = grid.GetCellPublic(pos);
                    if (cell == null || cell.cellType != Grid3D.CellType.Normal || cell.elementInfo == null)
                        continue;

                    if (!ContainsType(acceptedTypes, cell.elementInfo.powerUpType))
                        continue;

                    GridElement element = grid.GetElementAt(pos);
                    if (element != null)
                        candidates.Add(element.gameObject);
                }
            }

            if (candidates.Count == 0)
                return new GameObject[0];

            return new[] { candidates[UnityEngine.Random.Range(0, candidates.Count)] };
        }

        private static Match3Grid GetGrid()
        {
            if (GameManager.Instance == null)
                return null;

            return GameManager.Instance.CurrentLevel is LevelScene_Match3Game level
                ? level.grid as Match3Grid
                : null;
        }

        private static bool ContainsType(ElementPowerUpType[] acceptedTypes, ElementPowerUpType value)
        {
            for (int i = 0; i < acceptedTypes.Length; i++)
            {
                if (acceptedTypes[i] == value)
                    return true;
            }

            return false;
        }
    }

    [Serializable]
    public class ActionButton_Highlight : HighlightSelector
    {
        [ValueDropdown(nameof(GetAllActions))]
        [SerializeReference]
        public ActionBarItem action;

        public override GameObject[] HighlightedObjects
        {
            get
            {
                ActionBarManager actionBarManager = GetActionBarManager();

                for (int i = 0; i < actionBarManager.actionBarViews.Count; i++)
                {
                    ActionBarView view = actionBarManager.actionBarViews[i];
                    if (view == null || view.actionBarItem == null)
                        continue;

                    if (view.actionBarItem.ActionName == action.ActionName)
                        return new[] { view.useButton.gameObject };
                }

                return new GameObject[0];
            }
        }

        private static ActionBarManager GetActionBarManager()
        {
            if (GameManager.Instance == null)
                return null;

            return GameManager.Instance.actionBarManager;
        }

        private IEnumerable GetAllActions()
        {
            ActionBarManager actionBarManager = GetActionBarManager();
            ValueDropdownList<ActionBarItem> valueDropdownItems = new();
            foreach (ActionBarItem item in actionBarManager.actionBarItemList)
            {
                valueDropdownItems.Add(item.ActionName, item);
            }
            return valueDropdownItems;
        }
    }
}
