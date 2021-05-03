using System;
using UnityEngine;

namespace BrunoMikoski.Pooling
{
    public sealed class PoolMember : MonoBehaviour
    {
        private bool isFirstSpawn = true;
        public bool IsFirstSpawn => isFirstSpawn;

        private bool isSpawned;
        public bool IsSpawned => isSpawned;

        private IOnDespawn[] onDespawnComponents;
        private IOnPool[] onPoolComponents;
        private IOnSpawn[] onSpawnComponents;

        private Pool pool;
        public Pool Pool => pool;
        private bool destroyInternal;

        internal void Initialize(Pool pool)
        {
            this.pool = pool;
            onPoolComponents = GetComponentsInChildren<IOnPool>();
            onSpawnComponents = GetComponentsInChildren<IOnSpawn>();
            onDespawnComponents = GetComponentsInChildren<IOnDespawn>();

            for (int i = 0; i < onPoolComponents.Length; i++)
                if (onPoolComponents[i] != null)
                    onPoolComponents[i].OnPool();
        }

        internal void OnSpawn()
        {
            isSpawned = true;
            for (int i = 0; i < onSpawnComponents.Length; i++)
            {
                if (onSpawnComponents[i] != null)
                    onSpawnComponents[i].OnSpawn();
            }
        }

        internal void OnDespawn()
        {
            for (int i = 0; i < onDespawnComponents.Length; i++)
            {
                if (onDespawnComponents[i] != null)
                    onDespawnComponents[i].OnDespawn();
            }
            isSpawned = false;
            isFirstSpawn = false;
        }

        private void OnDestroy()
        {
            if (SimplePool.IsApplicationQuiting)
                return;
            
            SimplePool.UnregisterPoolMember(this);

            if (!destroyInternal && !SimplePool.IsApplicationQuiting && pool != null && !pool.AllowDestroying)
            {
                if (pool.Persistent)
                {
                    if (!SimplePoolSettings.Instance.AllowDestructionOfPooledItems)
                    {
                        throw new Exception(
                            $"Object {this.gameObject.name} been destroyed while is still in the pool, use SimplePool.Despawn "
                            + "or SimplePool.DestroyPool instead");
                    }
                }
            }
        }

        internal void Despawn()
        {
            if (pool != null)
            {
                pool.Despawn(this);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        public void DestroyInternal()
        {
            destroyInternal = true;
            Destroy(gameObject);
        }
    }
}
