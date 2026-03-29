using UnityEngine;
using UnityEngine.Events;

namespace Game
{

    public abstract class HighlightSelector
    {
        public abstract GameObject[] HighlightedObjects { get; }
    }

    public class FirstMatchableElement_Highlight : HighlightSelector
    {
        // Adjacent directions — right and up only so each pair is visited once.
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

        // ------------------------------------------------------------------
        //  Grid access
        // ------------------------------------------------------------------

        private static Match3Grid GetGrid()
        {
            if (GameManager.Instance == null) return null;
            return GameManager.Instance.CurrentLevel is LevelScene_Match3Game lvl
                ? lvl.grid as Match3Grid
                : null;
        }

        // ------------------------------------------------------------------
        //  Possible-match detection
        // ------------------------------------------------------------------

        private static bool SwapWouldMatch(Match3Grid grid, Vector2Int a, Vector2Int b)
        {
            Grid3D.GridCell cellA = grid.GetCellPublic(a);
            Grid3D.GridCell cellB = grid.GetCellPublic(b);

            if (!IsMatchable(cellA) || !IsMatchable(cellB)) return false;

            ElementData dataA = cellA.elementInfo.elementData;
            ElementData dataB = cellB.elementInfo.elementData;

            if (dataA == dataB) return false;

            cellA.elementInfo.elementData = dataB;
            cellB.elementInfo.elementData = dataA;

            bool matches = CreatesLineMatchAt(grid, a) || CreatesLineMatchAt(grid, b);

            cellA.elementInfo.elementData = dataA;
            cellB.elementInfo.elementData = dataB;

            return matches;
        }

        /// <summary>
        /// Returns the two positions ordered so that index 0 is the element that
        /// <b>moves into</b> the match (the one the player should drag), and index 1
        /// is the element it should be swapped with.
        /// Returns false when the swap produces no match.
        /// </summary>
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

            // Simulate swap once.
            cellA.elementInfo.elementData = dataB;
            cellB.elementInfo.elementData = dataA;

            // A lands at B's position -> A is mover when B position forms a line.
            bool aIsMover = CreatesLineMatchAt(grid, b);

            // B lands at A's position -> B is mover when A position forms a line.
            bool bIsMover = CreatesLineMatchAt(grid, a);

            // Restore board.
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

        // ------------------------------------------------------------------
        //  Result collection
        // ------------------------------------------------------------------

        private static GameObject[] CollectGameObjects(Match3Grid grid,
                                                       Vector2Int a, Vector2Int b)
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
}
