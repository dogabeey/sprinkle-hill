using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using UnityEngine; using Game.EventManagement;

namespace Game
{
    public interface IPoolable<T> where T : Behaviour
    {
        void OnSpawn();
        void OnDespawn();
    }

    public class PoolingManager : SerializedMonoBehaviour
    {
        /// <summary>
        /// Dictionary to hold pools for different types of objects. Each pool is a queue of available instances of that type.
        /// </summary>
        [SerializeField]
        public Dictionary<string, Queue<Behaviour>> poolsDicitonary = new Dictionary<string, Queue<Behaviour>>();

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
        
        }

        // Update is called once per frame
        void Update()
        {
        
        }

        public T Spawn<T>(T prefab, Vector3 position, Quaternion rotation, Transform parent) where T : Behaviour
        {
            // Implement pooling logic here (e.g., check for available instances in the pool, instantiate if necessary)
            if (!GetFromPool(out T instance))
            {
                instance = Instantiate(prefab, position, rotation, parent);
            }
            if (instance is IPoolable<T> poolable)
            {
                poolable.OnSpawn();
            }
            return instance;
        }
        public T Spawn<T>(T prefab, Vector3 position, Quaternion rotation) where T : Behaviour
        {
            return Spawn(prefab, position, rotation, null);
        }
        public T Spawn<T>(T prefab, Vector3 position) where T : Behaviour
        {
            return Spawn(prefab, position, Quaternion.identity, null);
        }

        public void Despawn<T>(T instance) where T : Behaviour
        {
            if (instance is IPoolable<T> poolable)
            {
                poolable.OnDespawn();
            }
            // Implement pooling logic here (e.g., return the instance to the pool instead of destroying it)
            ReturnToPool(instance);
        }

        private void ReturnToPool<T>(T instance) where T : Behaviour
        {
            string type = typeof(T).Name;
            if (!poolsDicitonary.ContainsKey(type))
            {
                poolsDicitonary[type] = new Queue<Behaviour>();
            }
            poolsDicitonary[type].Enqueue(instance);
            instance.gameObject.SetActive(false);
        }
        private bool GetFromPool<T>(out T instance) where T : Behaviour
        {
            string type = typeof(T).Name;
            if (poolsDicitonary.ContainsKey(type) && poolsDicitonary[type].Count > 0)
            {
                instance = (T)poolsDicitonary[type].Dequeue();
                instance.gameObject.SetActive(true);
                return true;
            }
            instance = null;
            return false;
        }
    }
}
