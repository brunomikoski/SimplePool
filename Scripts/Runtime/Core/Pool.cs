using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
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

        //The amount of objects to be pooled at startup time
        private int initialQuantity;

        //The max amount of pooled objects (value of 0 or lower = infinite)
        private int maxQuantity;

        // We append an id to the name of anything we instantiate. This is purely cosmetic.
        private int nextId = 1;

        // The prefab that we are pooling
        private GameObject prefab;
        public GameObject Prefab
        {
            get { return prefab; }
        }

        private Scene? scene;

        public bool Persistent
        {
            get { return !scene.HasValue; }
        }

        private bool allowDestroying;
        public bool AllowDestroying
        {
            get { return allowDestroying; }
        }

        public int TotalObjectCount
        {
            get { return inactive.Count + active.Count; }
        }


        public void Initialize(GameObject prefab, int initialQty, bool allowDestroying)
        {
            this.prefab = prefab;

            PoolSettings poolSettings = prefab.GetComponent<PoolSettings>();
            if (poolSettings != null)
            {
                initialQty = poolSettings.PoolSize;
                maxQuantity = poolSettings.MaxPoolSize;
            }

            initialQuantity = initialQty;

            inactive = new List<PoolMember>(initialQuantity);
            active = new List<PoolMember>(initialQuantity);
            this.allowDestroying = allowDestroying;

            AddObjectsToPool(initialQuantity);
        }

        public void SetScene(Scene targetScene)
        {
            scene = targetScene;
        }

        private void SetPoolDisplayName()
        {
            gameObject.name = string.Format("{0} Pool - Size: {1} - [{2} -> {3}]", prefab.name, nextId,
                initialQuantity, createdAfterInitialSetup);
        }

        private void AddObjectsToPool(int amount)
        {
            for (int i = 0; i < amount; i++)
                AddObjectToPool();
        }

        internal PoolMember AddObjectToPool()
        {
            GameObject obj = Instantiate(prefab, transform, false);
            obj.SetActive(false);
            obj.name = prefab.name + " (" + (nextId++) + ")";

            SetPoolDisplayName();

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

        internal PoolMember Spawn(Transform parent, float activeDuration)
        {
            PoolMember poolMember = GetPoolMember();

            poolMember.transform.SetParent(parent, false);

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
            for (int i = 0; i < active.Count; i++)
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
            for (int i = 0; i < active.Count; i++)
                active[i].DestroyInternal();

            for (int i = 0; i < inactive.Count; i++)
                inactive[i].DestroyInternal();

            Object.Destroy(gameObject);
        }

        public bool UnregisterMember(PoolMember poolMember)
        {
            return (active.Remove(poolMember) || active.Remove(poolMember));
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
    }
}
