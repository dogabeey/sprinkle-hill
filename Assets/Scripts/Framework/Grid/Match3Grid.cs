using UnityEngine;
using DG.Tweening;

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
    }
}
