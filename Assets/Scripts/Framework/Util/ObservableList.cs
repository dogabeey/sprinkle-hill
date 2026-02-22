using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Game
{
    public class ObservableList<T> : List<T>
    {
        public delegate void OnAdd(T item);
        public delegate void OnRemove(T item);
        public delegate void OnClear();
        public event OnAdd onAdd;
        public event OnRemove onRemove;
        public event OnClear onClear;

        public new void Add(T item)
        {
            base.Add(item);
            onAdd?.Invoke(item);
        }
        public new void Remove(T item)
        {
            base.Remove(item);
            onRemove?.Invoke(item);
        }
        public new void Clear()
        {
            base.Clear();
            onClear?.Invoke();
        }   
    }
}
