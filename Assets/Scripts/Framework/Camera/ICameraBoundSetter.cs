using UnityEngine; using Game.EventManagement;

namespace Game
{
    public interface ICameraBoundSetter
    {
        Vector2 CameraBound { get; }
    }
}
