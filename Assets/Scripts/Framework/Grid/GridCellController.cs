using UnityEngine;

namespace Game
{
    public class GridCellController : MonoBehaviour, ICameraBoundSetter
    {
        public Vector2Int Coordinates { get; private set; }
        public Vector2 CameraBound => new Vector2(transform.position.x, transform.position.y);

        public virtual void Bind(Vector2Int coordinates)
        {
            Coordinates = coordinates;
        }
    }
}
