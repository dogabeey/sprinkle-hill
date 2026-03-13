using System.Collections;
using UnityEngine;

namespace Game
{
    public class BreakableWall : GridCellController
    {
        public IEnumerator WallBreak()
        {
            yield break;
        }
    }
}
