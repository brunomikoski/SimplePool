using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.SceneManagement;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace BrunoMikoski.Pooling
{
    public sealed class Pool : MonoBehaviour
    {
        // For statistics it's interesting to know how many were created during the game,
        // if this is a large number, you could consider increasing the initial quantity
        private int createdAfterInitialSetup;

        private List<PoolMember> inactive;
        private List<PoolMember> active;

        
        public int UnusedCount
        {
            get
            {
                if (inactive == null)
                    return 0;
                return inactive.Count;
            }
        }

        //The amount of objects to be pooled at startup time
        private int initialQuantity;

        //The max amount of pooled objects (value of 0 or lower = infinite)
        private int maxQuantity;

        // We append an id to the name of anything we instantiate. This is purely cosmetic.
        private int nextId = 1;

        // The prefab that we are pooling
        public GameObject Prefab { get; private set; }

        private Scene? scene;

        public bool Persistent => !scene.HasValue;

        private bool allowDestroying;
        public bool AllowDestroying => allowDestroying;

        public int TotalObjectCount => inactive.Count + active.Count;


        public void Initialize(GameObject targetPrefab, int initialQty, bool targetAllowDestroying)
        {
            Prefab = targetPrefab;

            PoolSettings poolSettings = targetPrefab.GetComponent<PoolSettings>();
            if (poolSettings != null)
            {
                initialQty = poolSettings.PoolSize;
                maxQuantity = poolSettings.MaxPoolSize;
            }

            initialQuantity = initialQty;

            inactive = new List<PoolMember>(initialQuantity);
            active = new List<PoolMember>(initialQuantity);
            allowDestroying = targetAllowDestroying;

            AddObjectsToPool(initialQuantity);
        }

        public void SetScene(Scene targetScene)
        {
            scene = targetScene;
        }

        [Conditional("UNITY_EDITOR")]
        private void SetPoolDisplayName()
        {
            gameObject.name = string.Format("{0} Pool - Size: {1} - [{2} -> {3}]", Prefab.name, nextId,
                initialQuantity, createdAfterInitialSetup);
        }

        private void AddObjectsToPool(int amount)
        {
            for (int i = 0; i < amount; i++)
                AddObjectToPool();
            SetPoolDisplayName();
        }

        internal PoolMember AddObjectToPool()
        {
            GameObject obj = Instantiate(Prefab, transform, false);
            obj.SetActive(false);
#if UNITY_EDITOR
            obj.name = Prefab.name + " (" + (nextId++) + ")";
#endif

            PoolMember poolMember = obj.AddComponent<PoolMember>();
            poolMember.Initialize(this);
            SimplePool.RegisterPoolMember(poolMember);
            inactive.Add(poolMember);

            return poolMember;
        }

        internal PoolMember Spawn(Vector3 pos, Quaternion rot)
        {
            PoolMember poolMember = GetPoolMember();

            poolMember.transform.position = pos;
            poolMember.transform.rotation = rot;

            ReadyPoolMember(poolMember);
            return poolMember;
        }

        internal PoolMember Spawn(Transform parent, Vector3? position, Quaternion? rotation)
        {
            PoolMember poolMember = GetPoolMember();

            if (parent != null)
                poolMember.transform.SetParent(parent);

            if (position.HasValue && rotation.HasValue)
            {
                poolMember.transform.SetPositionAndRotation(position.Value, rotation.Value);
            }
            else
            {
                if (position.HasValue)
                    poolMember.transform.position = position.Value;

                if (rotation.HasValue)
                    poolMember.transform.rotation = rotation.Value;
            }
            ReadyPoolMember(poolMember);
            return poolMember;
        }

        private PoolMember GetPoolMember()
        {
            EnsurePoolIsNotEmpty();
            PoolMember poolMember = inactive[0];
            inactive.RemoveAt(0);

            if (poolMember == null)
            {
#if DEBUG
                Debug.LogWarning("Retrieved pool member was empty, getting new!");
#endif
                return GetPoolMember();
            }

            active.Add(poolMember);
            return poolMember;
        }

        private void EnsurePoolIsNotEmpty()
        {
            if (inactive.Count != 0)
                return;

            //Check if the maximum amount of pooled objects is in use
            if (maxQuantity > 0 && TotalObjectCount >= maxQuantity)
            {
                RecycleActiveObject();
            }
            else
            {
                //Add new instance
                createdAfterInitialSetup += 1;
                AddObjectToPool();
                SetPoolDisplayName();
            }
        }

        private void RecycleActiveObject()
        {
            if (active.Count == 0)
                return;

            active[0].Despawn();
        }

        private void ReadyPoolMember(PoolMember poolMember)
        {
            poolMember.gameObject.SetActive(true);
            poolMember.OnSpawn();
        }

        private void DespawnAll()
        {
            for (int i = active.Count - 1; i >= 0; i--)
                Despawn(active[i]);
        }

        internal void Despawn(PoolMember poolMember)
        {
#if DEBUG  // to save performance on release builds, only do this check in debug mode
            if (inactive.Contains(poolMember))
            {
                Debug.LogErrorFormat(poolMember, "Pool member '{0}' was already despawned", poolMember.name);
                return;
            }
#endif
            poolMember.gameObject.SetActive(false);
            poolMember.transform.SetParent(transform, false);
            poolMember.OnDespawn();

            active.Remove(poolMember);
            inactive.Add(poolMember);
        }

        public void Destroy()
        {
            for (int i = active.Count - 1; i >= 0; i--)
            {
                PoolMember poolMember = active[i];
                if (poolMember != null)
                    poolMember.DestroyInternal();
            }

            for (int i = inactive.Count - 1; i >= 0; i--)
            {
                PoolMember poolMember = inactive[i];
                if (poolMember != null)
                    poolMember.DestroyInternal();
            }

            Object.Destroy(gameObject);
        }

        public bool UnregisterMember(PoolMember poolMember)
        {
            return (active.Remove(poolMember) || inactive.Remove(poolMember));
        }

        private void OnDestroy()
        {
            SimplePool.UnregisterPool(this);
        }

        public void OnBeforeSceneUnload(Scene targetScene)
        {
            for (int i = active.Count - 1; i >= 0; i--)
            {
                PoolMember poolMember = active[i];
                if (poolMember.gameObject.scene != targetScene)
                    continue;

                Despawn(poolMember);
            }
        }

        public void EnsurePoolSize(int targetPoolSize)
        {
            if (targetPoolSize < 0)
                targetPoolSize = 0;

            int currentCount = TotalObjectCount;
            if (currentCount == targetPoolSize)
                return;

            if (currentCount < targetPoolSize)
            {
                AddObjectsToPool(targetPoolSize - currentCount);
                return;
            }

            int toRemove = Mathf.Min(currentCount - targetPoolSize, inactive.Count);
            for (int i = 0; i < toRemove; i++)
            {
                int lastIndex = inactive.Count - 1;
                PoolMember poolMember = inactive[lastIndex];
                inactive.RemoveAt(lastIndex);
                poolMember.DestroyInternal();
            }

            SetPoolDisplayName();
        }
    }
}
