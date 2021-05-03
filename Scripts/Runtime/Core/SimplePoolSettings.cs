using UnityEngine;

namespace BrunoMikoski.Pooling
{
    public class SimplePoolSettings : ResourceScriptableObjectSingleton<SimplePoolSettings>
    {
        [SerializeField]
        private int defaultPoolSize = 3;
        public int DefaultPoolSize => defaultPoolSize;

        [SerializeField]
        private bool despawnOnSceneUnload;
        public bool DespawnOnSceneUnload => despawnOnSceneUnload;

        [SerializeField]
        private bool allowDestructionOfPooledItems;
        public bool AllowDestructionOfPooledItems => allowDestructionOfPooledItems;

        public void SetDefaultPoolSize(int poolSize)
        {
            defaultPoolSize = poolSize;
        }

        public void SetDespawnOnSceneUnload(bool shouldDespawn)
        {
            despawnOnSceneUnload = shouldDespawn;
        }
    }
}
