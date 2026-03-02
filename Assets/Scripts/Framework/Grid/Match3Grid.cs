using UnityEngine;
using DG.Tweening;
using System.Collections;
using System.Collections.Generic;

namespace Game
{
    /// <summary>
    /// Represents a grid specialized for Match-3 gameplay.
    /// </summary>
    public class Match3Grid : Grid3D
    {
        public override void PreInit()
        {
            // Custom pre-initialization logic for Match-3 grid
        }
        public override void PostInit()
        {
            // Custom post-initialization logic for Match-3 grid
        }

        public bool TryGetElementPosition(GridElement element, out Vector2Int position)
        {
            if (element != null)
            {
                Transform elementParent = element.transform.parent;
                foreach (var tileKvp in generatedTiles)
                {
                    if (tileKvp.Value == elementParent)
                    {
                        position = new Vector2Int(tileKvp.Key.x, tileKvp.Key.y);
                        return true;
                    }
                }
            }

            position = default;
            return false;
        }

        public static bool AreAdjacent(Vector2Int first, Vector2Int second)
        {
            return Mathf.Abs(first.x - second.x) + Mathf.Abs(first.y - second.y) == 1;
        }

        public IEnumerator SwapAndMatch(Vector2Int first, Vector2Int second)
        {
            yield return StartCoroutine(SwapElements(first, second));
            yield return StartCoroutine(MatchProcess(first, second));
        }

        public IEnumerator MatchProcess(Vector2Int initialElement1, Vector2Int initialElement2)
        {
            List<List<Vector2Int>> matchedGroups;
            int matchComboCount = 0;
            // 1. Check for matches
            while ((matchedGroups = CheckMatchOf(3)).Count > 0)
            {
                matchComboCount++;
                // 2. Clear matched elements with animations
                yield return StartCoroutine(ClearMatches(matchedGroups));
                // 3. Apply gravity and refill
                yield return StartCoroutine(ApplyGravity());
            }

            if(matchComboCount == 0)
            {
                // If no matches were found, swap the elements back to their original positions
                yield return StartCoroutine(SwapElements(initialElement1, initialElement2));
            }

            yield break;
        }

        public IEnumerator SwapElements(Vector2Int first, Vector2Int second)
        {
            Vector2Int firstPos = new Vector2Int(first.x, first.y);
            Vector2Int secondPos = new Vector2Int(second.x, second.y);

            GridCell firstCell = GetCell(firstPos);
            GridCell secondCell = GetCell(secondPos);

            if (firstCell == null || secondCell == null)
            {
                yield break;
            }

            GridElementInfo firstInfo = firstCell.elementInfo;
            GridElementInfo secondInfo = secondCell.elementInfo;

            if (firstInfo == null || secondInfo == null)
            {
                yield break;
            }

            firstCell.elementInfo = secondInfo;
            secondCell.elementInfo = firstInfo;

            if (!generatedTiles.TryGetValue(firstPos, out Transform firstTile) ||
                !generatedTiles.TryGetValue(secondPos, out Transform secondTile))
            {
                yield break;
            }

            GridElement firstElement = firstTile.GetComponentInChildren<GridElement>();
            GridElement secondElement = secondTile.GetComponentInChildren<GridElement>();

            if (firstElement != null && secondElement != null)
            {
                Transform firstParent = firstElement.transform.parent;
                Transform secondParent = secondElement.transform.parent;

                firstElement.transform.SetParent(secondParent, false);
                secondElement.transform.SetParent(firstParent, false);

                firstElement.transform.DOLocalMove(Vector3.zero, GameManager.Instance.constantManager.elementSwapMoveDuration).SetEase(Ease.OutBack);
                yield return secondElement.transform.DOLocalMove(Vector3.zero, GameManager.Instance.constantManager.elementSwapMoveDuration).SetEase(Ease.OutBack).WaitForCompletion();
            }
            else if (firstElement != null)
            {
                firstElement.transform.SetParent(secondTile, false);
                yield return firstElement.transform.DOLocalMove(Vector3.zero, GameManager.Instance.constantManager.elementSwapMoveDuration).SetEase(Ease.OutBack).WaitForCompletion();
            }
            else if (secondElement != null)
            {
                secondElement.transform.SetParent(firstTile, false);
                yield return secondElement.transform.DOLocalMove(Vector3.zero, GameManager.Instance.constantManager.elementSwapMoveDuration).SetEase(Ease.OutBack).WaitForCompletion();
            }
        }
        private List<List<Vector2Int>> CheckMatchOf(int elementCount = 3)
        {
            Dictionary<Vector2Int, ElementData> matchedElements = new Dictionary<Vector2Int, ElementData>();

            ElementData GetElementData(int x, int y)
            {
                GridCell cell = GetCell(new Vector2Int(x, y));
                return cell != null ? cell.elementInfo?.elementData : null;
            }

            void AddMatched(int x, int y, ElementData data)
            {
                Vector2Int pos = new Vector2Int(x, y);
                if (!matchedElements.ContainsKey(pos))
                {
                    matchedElements.Add(pos, data);
                }
            }

            for (int y = 0; y < gridSize.y; y++)
            {
                ElementData currentData = null;
                int runLength = 0;

                for (int x = 0; x < gridSize.x; x++)
                {
                    ElementData data = GetElementData(x, y);

                    if (data != null && data == currentData)
                    {
                        runLength++;
                        continue;
                    }

                    if (currentData != null && runLength >= elementCount)
                    {
                        for (int matchX = x - runLength; matchX < x; matchX++)
                        {
                            AddMatched(matchX, y, currentData);
                        }
                    }

                    currentData = data;
                    runLength = data != null ? 1 : 0;
                }

                if (currentData != null && runLength >= elementCount)
                {
                    for (int matchX = gridSize.x - runLength; matchX < gridSize.x; matchX++)
                    {
                        AddMatched(matchX, y, currentData);
                    }
                }
            }

            for (int x = 0; x < gridSize.x; x++)
            {
                ElementData currentData = null;
                int runLength = 0;

                for (int y = 0; y < gridSize.y; y++)
                {
                    ElementData data = GetElementData(x, y);

                    if (data != null && data == currentData)
                    {
                        runLength++;
                        continue;
                    }

                    if (currentData != null && runLength >= elementCount)
                    {
                        for (int matchY = y - runLength; matchY < y; matchY++)
                        {
                            AddMatched(x, matchY, currentData);
                        }
                    }

                    currentData = data;
                    runLength = data != null ? 1 : 0;
                }

                if (currentData != null && runLength >= elementCount)
                {
                    for (int matchY = gridSize.y - runLength; matchY < gridSize.y; matchY++)
                    {
                        AddMatched(x, matchY, currentData);
                    }
                }
            }

            List<List<Vector2Int>> groups = new List<List<Vector2Int>>();
            HashSet<Vector2Int> visited = new HashSet<Vector2Int>();

            foreach (var kvp in matchedElements)
            {
                if (visited.Contains(kvp.Key))
                {
                    continue;
                }

                List<Vector2Int> group = new List<Vector2Int>();
                Queue<Vector2Int> queue = new Queue<Vector2Int>();
                queue.Enqueue(kvp.Key);
                visited.Add(kvp.Key);

                while (queue.Count > 0)
                {
                    Vector2Int pos = queue.Dequeue();
                    group.Add(pos);
                    ElementData data = matchedElements[pos];

                    Vector2Int[] neighbors =
                    {
                        new Vector2Int(pos.x + 1, pos.y),
                        new Vector2Int(pos.x - 1, pos.y),
                        new Vector2Int(pos.x, pos.y + 1),
                        new Vector2Int(pos.x, pos.y - 1)
                    };

                    foreach (Vector2Int neighbor in neighbors)
                    {
                        if (matchedElements.TryGetValue(neighbor, out ElementData neighborData) &&
                            neighborData == data &&
                            !visited.Contains(neighbor))
                        {
                            visited.Add(neighbor);
                            queue.Enqueue(neighbor);
                        }
                    }
                }

                if (group.Count >= elementCount)
                {
                    groups.Add(group);
                }
            }

            return groups;
        }
        // Clears matched elements from the grid and plays their destruction animations.
        private IEnumerator ClearMatches(List<List<Vector2Int>> matchedPositions)
        {
            HashSet<Vector2Int> clearedPositions = new HashSet<Vector2Int>();
            foreach (var group in matchedPositions)
            {
                foreach (var pos in group)
                {
                    if (clearedPositions.Contains(pos))
                    {
                        continue;
                    }
                    Vector2Int gridPos = new Vector2Int(pos.x, pos.y);
                    GridCell cell = GetCell(gridPos);
                    if (cell != null && cell.elementInfo != null)
                    {
                        cell.elementInfo = null;
                        if (generatedTiles.TryGetValue(gridPos, out Transform tile))
                        {
                            GridElement element = tile.GetComponentInChildren<GridElement>();
                            if (element != null)
                            {
                                yield return element.DestroyElement();
                            }
                        }
                    }
                    clearedPositions.Add(pos);
                }
                yield return new WaitForSeconds(GameManager.Instance.constantManager.matchClearDelay);
            }
        }
        private IEnumerator ApplyGravity()
        {
            ConstantManager constantManager = GameManager.Instance != null ? GameManager.Instance.constantManager : null;
            float moveDuration = constantManager != null ? constantManager.elementSwapMoveDuration : 0.3f;

            EnsureGridCells();
            List<ElementData> elementPool = new List<ElementData>();
            for (int x = 0; x < gridSize.x; x++)
            {
                for (int y = 0; y < gridSize.y; y++)
                {
                    GridCell cell = GetCell(new Vector2Int(x, y));
                    ElementData data = cell != null ? cell.elementInfo?.elementData : null;
                    if (data != null && !elementPool.Contains(data))
                    {
                        elementPool.Add(data);
                    }
                }
            }

            if (elementPool.Count == 0)
            {
                foreach (GridElement element in generatedElements)
                {
                    if (element != null && element.elementInfo != null && element.elementInfo.elementData != null && !elementPool.Contains(element.elementInfo.elementData))
                    {
                        elementPool.Add(element.elementInfo.elementData);
                    }
                }
            }

            if (elementPool.Count == 0)
            {
                yield break;
            }

            Sequence gravitySequence = DOTween.Sequence();
            bool hasTween = false;
            float spawnStep = 1f;

            for (int x = 0; x < gridSize.x; x++)
            {
                List<int> playableRows = new List<int>();
                for (int y = 0; y < gridSize.y; y++)
                {
                    Vector2Int cellPos = new Vector2Int(x, y);
                    GridCell cell = GetCell(cellPos);
                    if (cell != null && cell.cellType == CellType.Normal)
                    {
                        playableRows.Add(y);
                    }
                }

                if (playableRows.Count == 0)
                {
                    continue;
                }

                int writeIndex = playableRows.Count - 1;

                for (int readIndex = playableRows.Count - 1; readIndex >= 0; readIndex--)
                {
                    int readY = playableRows[readIndex];
                    Vector2Int readPos = new Vector2Int(x, readY);

                    GridCell readCell = GetCell(readPos);
                    GridElementInfo movingInfo = readCell != null ? readCell.elementInfo : null;
                    if (movingInfo == null || movingInfo.elementData == null)
                    {
                        continue;
                    }

                    int targetY = playableRows[writeIndex];
                    Vector2Int targetPos = new Vector2Int(x, targetY);
                    GridCell targetCell = GetCell(targetPos);

                    if (targetCell == null)
                    {
                        continue;
                    }

                    if (targetPos != readPos)
                    {
                        readCell.elementInfo = null;
                        targetCell.elementInfo = movingInfo;

                        if (generatedTiles.TryGetValue(readPos, out Transform fromTile) && generatedTiles.TryGetValue(targetPos, out Transform toTile))
                        {
                            GridElement movingElement = fromTile.GetComponentInChildren<GridElement>();
                            if (movingElement != null)
                            {
                                movingElement.transform.SetParent(toTile, false);
                                gravitySequence.Join(movingElement.transform.DOLocalMove(Vector3.zero, moveDuration).SetEase(Ease.OutQuad));
                                hasTween = true;
                            }
                        }
                    }

                    writeIndex--;
                }

                int emptyCount = writeIndex + 1;
                for (int emptyIndex = writeIndex; emptyIndex >= 0; emptyIndex--)
                {
                    int targetY = playableRows[emptyIndex];
                    Vector2Int targetPos = new Vector2Int(x, targetY);
                    GridCell targetCell = GetCell(targetPos);

                    if (targetCell == null)
                    {
                        continue;
                    }

                    ElementData randomData = elementPool[Random.Range(0, elementPool.Count)];
                    GridElementInfo newInfo = new GridElementInfo { elementData = randomData };
                    targetCell.elementInfo = newInfo;

                    if (generatedTiles.TryGetValue(targetPos, out Transform targetTile))
                    {
                        int stackOffset = emptyCount - emptyIndex;
                        Vector3 spawnWorldPos = targetTile.position + Vector3.up * (spawnStep * stackOffset);
                        GridElement newElement = Instantiate(gridElementPrefab, spawnWorldPos, Quaternion.identity, targetTile);
                        newElement.elementInfo = newInfo;
                        generatedElements.Add(newElement);
                        newElement.InitElement(this, newInfo);

                        gravitySequence.Join(newElement.transform.DOLocalMove(Vector3.zero, moveDuration).SetEase(Ease.OutQuad));
                        hasTween = true;
                    }
                }
            }

            generatedElements.RemoveAll(element => element == null);

            if (hasTween)
            {
                yield return gravitySequence.WaitForCompletion();
            }
        }
    }
}

namespace Game
{
}