using UnityEngine;
using DG.Tweening;
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

        public void MatchProcess()
        {

        }

        public void SwapElements(Vector2Int first, Vector2Int second)
        {
            Vector3Int firstPos = new Vector3Int(first.x, 0, first.y);
            Vector3Int secondPos = new Vector3Int(second.x, 0, second.y);

            bool hasFirst = gridElements.TryGetValue(firstPos, out GridElementInfo firstInfo);
            bool hasSecond = gridElements.TryGetValue(secondPos, out GridElementInfo secondInfo);

            if (!hasFirst || !hasSecond)
            {
                return;
            }

            gridElements.Remove(firstPos);
            gridElements.Remove(secondPos);
            gridElements[secondPos] = firstInfo;
            gridElements[firstPos] = secondInfo;

            if (!generatedTiles.TryGetValue(firstPos, out Transform firstTile) ||
                !generatedTiles.TryGetValue(secondPos, out Transform secondTile))
            {
                return;
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
                secondElement.transform.DOLocalMove(Vector3.zero, 0.3f).SetEase(Ease.OutBack);
            }
            else if (firstElement != null)
            {
                firstElement.transform.SetParent(secondTile, false);
                firstElement.transform.DOLocalMove(Vector3.zero, 0.3f).SetEase(Ease.OutBack);
            }
            else if (secondElement != null)
            {
                secondElement.transform.SetParent(firstTile, false);
                secondElement.transform.DOLocalMove(Vector3.zero, 0.3f).SetEase(Ease.OutBack);
            }
        }

        public List<List<Vector2Int>> CheckMatchOf(int elementCount = 3)
        {
            Dictionary<Vector2Int, ElementData> matchedElements = new Dictionary<Vector2Int, ElementData>();

            ElementData GetElementData(int x, int z)
            {
                return gridElements.TryGetValue(new Vector3Int(x, 0, z), out GridElementInfo info)
                    ? info.elementData
                    : null;
            }

            void AddMatched(int x, int z, ElementData data)
            {
                Vector2Int pos = new Vector2Int(x, z);
                if (!matchedElements.ContainsKey(pos))
                {
                    matchedElements.Add(pos, data);
                }
            }

            for (int z = 0; z < gridSize.z; z++)
            {
                ElementData currentData = null;
                int runLength = 0;

                for (int x = 0; x < gridSize.x; x++)
                {
                    ElementData data = GetElementData(x, z);

                    if (data != null && data == currentData)
                    {
                        runLength++;
                        continue;
                    }

                    if (currentData != null && runLength >= elementCount)
                    {
                        for (int matchX = x - runLength; matchX < x; matchX++)
                        {
                            AddMatched(matchX, z, currentData);
                        }
                    }

                    currentData = data;
                    runLength = data != null ? 1 : 0;
                }

                if (currentData != null && runLength >= elementCount)
                {
                    for (int matchX = gridSize.x - runLength; matchX < gridSize.x; matchX++)
                    {
                        AddMatched(matchX, z, currentData);
                    }
                }
            }

            for (int x = 0; x < gridSize.x; x++)
            {
                ElementData currentData = null;
                int runLength = 0;

                for (int z = 0; z < gridSize.z; z++)
                {
                    ElementData data = GetElementData(x, z);

                    if (data != null && data == currentData)
                    {
                        runLength++;
                        continue;
                    }

                    if (currentData != null && runLength >= elementCount)
                    {
                        for (int matchZ = z - runLength; matchZ < z; matchZ++)
                        {
                            AddMatched(x, matchZ, currentData);
                        }
                    }

                    currentData = data;
                    runLength = data != null ? 1 : 0;
                }

                if (currentData != null && runLength >= elementCount)
                {
                    for (int matchZ = gridSize.z - runLength; matchZ < gridSize.z; matchZ++)
                    {
                        AddMatched(x, matchZ, currentData);
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
    }
}
