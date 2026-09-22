using System.Collections.Generic;
using Game.Singleton;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Game
{
    /// <summary>
    /// Owns reusable runtime visuals. Grid elements are returned here rather
    /// than destroyed once their clear animation has completed.
    /// </summary>
    public class PoolingManager : SingletonComponent<PoolingManager>
    {
        [Title("Grid Element Pool")]
        [SerializeField] private GridElement elementPrefab;
        [SerializeField, Min(0)] private int initialElementPoolSize = 72;

        private readonly Dictionary<GridElement, Queue<GridElement>> elementPools = new Dictionary<GridElement, Queue<GridElement>>();
        private readonly Dictionary<GridElement, GridElement> sourcePrefabByInstance = new Dictionary<GridElement, GridElement>();
        private Transform inactiveElementRoot;

        protected override void Awake()
        {
            base.Awake();
            EnsureInactiveElementRoot();
            PrewarmElementPool();
        }

        /// <summary>Gets an element visual from the pool associated with its prefab.</summary>
        public GridElement SpawnElement(GridElement prefab, Vector3 position, Quaternion rotation, Transform parent)
        {
            GridElement sourcePrefab = prefab != null ? prefab : elementPrefab;
            if (sourcePrefab == null)
            {
                Debug.LogError("[PoolingManager] Cannot spawn a grid element because no prefab is assigned.");
                return null;
            }

            GridElement instance = GetElementFromPool(sourcePrefab) ?? CreateElementInstance(sourcePrefab);
            if (instance == null)
                return null;

            Transform instanceTransform = instance.transform;
            instanceTransform.SetParent(parent, true);
            instanceTransform.SetPositionAndRotation(position, rotation);
            instance.gameObject.SetActive(true);
            instance.OnSpawn();
            return instance;
        }

        /// <summary>Returns a previously spawned grid element visual to its pool.</summary>
        public void DespawnElement(GridElement instance)
        {
            if (instance == null)
                return;

            if (!sourcePrefabByInstance.TryGetValue(instance, out GridElement sourcePrefab) || sourcePrefab == null)
            {
                Destroy(instance.gameObject);
                return;
            }

            // Guard against a repeated release from two overlapping clear paths.
            if (!instance.gameObject.activeSelf)
                return;

            instance.OnDespawn();
            instance.transform.SetParent(EnsureInactiveElementRoot(), false);
            instance.gameObject.SetActive(false);
            GetOrCreatePool(sourcePrefab).Enqueue(instance);
        }

        private void PrewarmElementPool()
        {
            if (elementPrefab == null)
                return;

            Queue<GridElement> pool = GetOrCreatePool(elementPrefab);
            for (int i = pool.Count; i < initialElementPoolSize; i++)
            {
                GridElement instance = CreateElementInstance(elementPrefab);
                if (instance == null)
                    break;

                instance.transform.SetParent(EnsureInactiveElementRoot(), false);
                instance.gameObject.SetActive(false);
                pool.Enqueue(instance);
            }
        }

        private GridElement GetElementFromPool(GridElement sourcePrefab)
        {
            Queue<GridElement> pool = GetOrCreatePool(sourcePrefab);
            while (pool.Count > 0)
            {
                GridElement instance = pool.Dequeue();
                if (instance != null)
                    return instance;
            }

            return null;
        }

        private GridElement CreateElementInstance(GridElement sourcePrefab)
        {
            GridElement instance = Instantiate(sourcePrefab, EnsureInactiveElementRoot());
            // Capture the prefab-local transform before SpawnElement reparents it
            // with worldPositionStays. Otherwise a scaled board parent can become
            // the pooled instance's incorrect scale baseline.
            instance.CaptureInitialPoolState();
            sourcePrefabByInstance[instance] = sourcePrefab;
            return instance;
        }

        private Queue<GridElement> GetOrCreatePool(GridElement sourcePrefab)
        {
            if (!elementPools.TryGetValue(sourcePrefab, out Queue<GridElement> pool))
            {
                pool = new Queue<GridElement>();
                elementPools.Add(sourcePrefab, pool);
            }

            return pool;
        }

        private Transform EnsureInactiveElementRoot()
        {
            if (inactiveElementRoot != null)
                return inactiveElementRoot;

            Transform existing = transform.Find("Inactive Elements");
            if (existing != null)
            {
                inactiveElementRoot = existing;
                return inactiveElementRoot;
            }

            GameObject root = new GameObject("Inactive Elements");
            inactiveElementRoot = root.transform;
            inactiveElementRoot.SetParent(transform, false);
            return inactiveElementRoot;
        }
    }
}
