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

                            if (SwapWouldMatch(grid, posA, posB))
                                return CollectGameObjects(grid, posA, posB);
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

            // No point swapping identical elements.
            if (dataA == dataB) return false;

            // Temporarily apply the swap and check for a run of 3.
            return HasRunAfterSwap(grid, a, dataB, b) ||
                   HasRunAfterSwap(grid, b, dataA, a);
        }

        /// <summary>
        /// Returns true when placing <paramref name="newData"/> at <paramref name="pos"/>
        /// (treating <paramref name="swappedOut"/> as holding <paramref name="newData"/>)
        /// creates a horizontal or vertical run of 3+.
        /// </summary>
        private static bool HasRunAfterSwap(Match3Grid grid, Vector2Int pos,
                                            ElementData newData, Vector2Int swappedOut)
        {
            int h = CountLine(grid, pos, Vector2Int.right, newData, swappedOut)
                  + CountLine(grid, pos, Vector2Int.left,  newData, swappedOut) + 1;

            int v = CountLine(grid, pos, Vector2Int.up,   newData, swappedOut)
                  + CountLine(grid, pos, Vector2Int.down,  newData, swappedOut) + 1;

            return h >= 3 || v >= 3;
        }

        private static int CountLine(Match3Grid grid, Vector2Int origin, Vector2Int dir,
                                     ElementData data, Vector2Int swappedOut)
        {
            int count = 0;
            Vector2Int cur = origin + dir;
            while (true)
            {
                ElementData d = GetEffectiveData(grid, cur, swappedOut, data);
                if (d != data) break;
                count++;
                cur += dir;
            }
            return count;
        }

        private static ElementData GetEffectiveData(Match3Grid grid, Vector2Int pos,
                                                    Vector2Int substitutePos,
                                                    ElementData substituteData)
        {
            if (pos == substitutePos) return substituteData;
            Grid3D.GridCell cell = grid.GetCellPublic(pos);
            return IsMatchable(cell) ? cell.elementInfo.elementData : null;
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
