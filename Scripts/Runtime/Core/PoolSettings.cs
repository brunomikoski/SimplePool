using UnityEngine;

namespace BrunoMikoski.Pooling
{
    public class PoolSettings : MonoBehaviour
    {
        [SerializeField]
        private int poolSize = 1;
        public int PoolSize => poolSize;

        [SerializeField]
        private int maxPoolSize = -1;
        public int MaxPoolSize => maxPoolSize;
    }
}
