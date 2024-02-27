using UnityEngine;
using UnityEngine.Scripting;

namespace BrunoMikoski.Pooling
{
    [Preserve]
    [CreateAssetMenu(menuName = "Create SimplePoolSettings", fileName = "SimplePoolSettings", order = 0)]
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

        
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        private static void Initialize()
        {
            LoadOrCreateInstance<SimplePoolSettings>();
        }
        
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
