using UnityEngine;

namespace Game
{
    public class GridCellController : MonoBehaviour
    {
        public Vector2Int Coordinates { get; private set; }

        public virtual void Bind(Vector2Int coordinates)
        {
            Coordinates = coordinates;
        }
    }
}
