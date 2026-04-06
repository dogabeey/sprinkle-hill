using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Game
{
	public interface ISaveable
    {
        string SaveId { get; }
        SaveDataType SaveDataType { get; }
        Dictionary<string, object> Save();
        bool Load(System.Action onLoadSuccess, System.Action onLoadFail);
    }
}
