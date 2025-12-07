using System.Collections.Generic;
using UnityEngine;

namespace RTS.Core.Pooling
{
    using Services;

    /// <summary>
    /// Generic object pooling system to avoid frequent instantiation/destruction.
    /// Improves performance and reduces garbage collection.
    /// </summary>
    public class ObjectPool : MonoBehaviour, IPoolService
    {
        private class Pool
        {
            public Queue<GameObject> Available = new Queue<GameObject>();
            public HashSet<GameObject> Active = new HashSet<GameObject>();
            public GameObject Prefab;
            public Transform Parent;

            public Pool(GameObject prefab, Transform parent)
            {
                Prefab = prefab;
                Parent = parent;
            }
        }

        private readonly Dictionary<int, Pool> pools = new Dictionary<int, Pool>();
        private Transform poolRoot;

        private void Awake()
        {
            poolRoot = new GameObject("ObjectPools").transform;
            poolRoot.SetParent(transform);
        }

        /// <summary>
        /// Get an instance from the pool or create a new one.
        /// </summary>
        public T Get<T>(T prefab) where T : Component
        {
            var instanceId = prefab.GetInstanceID();

            if (!pools.ContainsKey(instanceId))
            {
                CreatePool(prefab.gameObject);
            }

            var pool = pools[instanceId];
            GameObject instance;

            if (pool.Available.Count > 0)
            {
                instance = pool.Available.Dequeue();
                instance.SetActive(true);
            }
            else
            {
                instance = Instantiate(pool.Prefab, pool.Parent);
            }

            pool.Active.Add(instance);
            return instance.GetComponent<T>();
        }

        /// <summary>
        /// Return an instance to the pool.
        /// </summary>
        public void Return<T>(T instance) where T : Component
        {
            if (instance == null) return;

            var gameObj = instance.gameObject;
            var prefabId = GetPrefabId(gameObj);

            if (prefabId == -1)
            {
                Destroy(gameObj);
                return;
            }

            if (pools.TryGetValue(prefabId, out var pool))
            {
                if (pool.Active.Remove(gameObj))
                {
                    gameObj.SetActive(false);
                    gameObj.transform.SetParent(pool.Parent);
                    pool.Available.Enqueue(gameObj);
                }
            }
        }

        /// <summary>
        /// Pre-warm a pool with a certain number of instances.
        /// </summary>
        public void Warmup<T>(T prefab, int count) where T : Component
        {
            var instanceId = prefab.GetInstanceID();

            if (!pools.ContainsKey(instanceId))
            {
                CreatePool(prefab.gameObject);
            }

            var pool = pools[instanceId];

            for (int i = 0; i < count; i++)
            {
                var instance = Instantiate(pool.Prefab, pool.Parent);
                instance.SetActive(false);
                
                // Tag with pool ID for later identification
                var poolable = instance.AddComponent<PoolableObject>();
                poolable.PoolId = instanceId;
                
                pool.Available.Enqueue(instance);
            }
        }

        /// <summary>
        /// Clear all pools and destroy all instances.
        /// </summary>
        public void Clear()
        {
            foreach (var pool in pools.Values)
            {
                while (pool.Available.Count > 0)
                {
                    var obj = pool.Available.Dequeue();
                    if (obj != null) Destroy(obj);
                }

                foreach (var obj in pool.Active)
                {
                    if (obj != null) Destroy(obj);
                }

                pool.Active.Clear();
            }

            pools.Clear();
        }

        private void CreatePool(GameObject prefab)
        {
            var instanceId = prefab.GetInstanceID();
            var poolParent = new GameObject($"Pool_{prefab.name}").transform;
            poolParent.SetParent(poolRoot);

            pools[instanceId] = new Pool(prefab, poolParent);
        }

        private int GetPrefabId(GameObject instance)
        {
            var poolable = instance.GetComponent<PoolableObject>();
            return poolable != null ? poolable.PoolId : -1;
        }

        private void OnDestroy()
        {
            Clear();
        }
    }

    /// <summary>
    /// Helper component to track which pool an object belongs to.
    /// </summary>
    internal class PoolableObject : MonoBehaviour
    {
        public int PoolId;
    }
}
