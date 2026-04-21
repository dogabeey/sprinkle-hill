using UnityEngine;
using UnityEngine.UI;

namespace Game
{
    public class GridCellController : MonoBehaviour, ICameraBoundSetter
    {
        public SpriteRenderer gridSprite;
        public SpriteRenderer upperBorder;
        public SpriteRenderer lowerBorder;
        public SpriteRenderer leftBorder;
        public SpriteRenderer rightBorder;
        public Vector2Int Coordinates { get; private set; }
        public Vector2 CameraBound => new Vector2(transform.position.x, transform.position.y);

        public virtual void Bind(Vector2Int coordinates)
        {
            Coordinates = coordinates;
            ClearBorders();
        }

        public void SetBorders(bool up, bool down, bool left, bool right)
        {
            if (upperBorder != null) upperBorder.enabled = down;
            if (lowerBorder != null) lowerBorder.enabled = up;
            if (leftBorder != null) leftBorder.enabled = left;
            if (rightBorder != null) rightBorder.enabled = right;
        }

        public void ClearBorders()
        {
            SetBorders(false, false, false, false);
        }
    }
}
