using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace BrunoMikoski.Pooling
{
    public static class SimplePool
    {
        private const int INITIAL_POOL_MEMBER_DICTIONARY_SIZE = 512;

        private static Transform parent;

        private static Dictionary<int, Pool> prefabIDToPool = new Dictionary<int, Pool>();
        public static Dictionary<int, Pool> PrefabIDToPool => prefabIDToPool;

        private static readonly Dictionary<int, PoolMember> instanceToPoolMember =
            new Dictionary<int, PoolMember>(INITIAL_POOL_MEMBER_DICTIONARY_SIZE);

        private static bool initialized = false;
        private static bool isApplicationQuiting;
        public static bool IsApplicationQuiting => isApplicationQuiting;

        private static int sceneUnloadCount;


        [RuntimeInitializeOnLoadMethod]
        private static void Initialize()
        {
            if (initialized)
                return;
            initialized = true;

            prefabIDToPool = new Dictionary<int, Pool>();
            parent = new GameObject("SimplePool Objects").transform;
            Application.quitting += OnApplicationQuiting;
            Object.DontDestroyOnLoad(parent);
        }

        private static void OnApplicationQuiting()
        {
            isApplicationQuiting = true;
            Application.quitting -= OnApplicationQuiting;

            if (SimplePoolSettings.Instance != null)
            {
                if (SimplePoolSettings.Instance.DespawnOnSceneUnload && sceneUnloadCount == 0)
                {
                    Debug.LogWarning("OnBeforeSceneUnload has never been called, "
                                     + "call SimplePool.OnBeforeSceneUnload before unloading a scene or disable "
                                     + "<b>despawnOnSceneUnload</b> on the SimplePool preferences (File -> Preferences -> Simple Pool)");
                }
            }
        }

        private static Pool GetOrCreatePool(GameObject prefab, int? quantity = null, Scene? targetScene = null, bool
            allowDestroying = false)
        {
            Initialize();

            if (prefabIDToPool.TryGetValue(prefab.GetInstanceID(), out Pool pool))
                return pool;

            if (!quantity.HasValue)
                quantity = SimplePoolSettings.Instance.DefaultPoolSize;

            GameObject newPoolGameObject = new GameObject();
            pool = newPoolGameObject.AddComponent<Pool>();
            pool.Initialize(prefab, quantity.Value, allowDestroying);

            if (targetScene.HasValue)
            {
                SceneManager.MoveGameObjectToScene(newPoolGameObject, targetScene.Value);
                pool.SetScene(targetScene.Value);
            }
            else
            {
                newPoolGameObject.transform.SetParent(parent);
            }

            RegisterPool(prefab, pool);
            return pool;
        }

        private static void RegisterPool(GameObject prefab, Pool pool)
        {
            prefabIDToPool[prefab.GetInstanceID()] = pool;
        }

        public static void UnregisterPool(Pool targetPool)
        {
            prefabIDToPool.Remove(targetPool.Prefab.GetInstanceID());
        }

        public static void AddObjectsToPool(Component component, int quantity = 1)
        {
            AddObjectsToPool(component.gameObject, quantity);
        }

        public static void AddObjectsToPool(GameObject prefab, int quantity = 1)
        {
            Pool pool = GetOrCreatePool(prefab);

            for (int i = 0; i < quantity; i++)
                pool.AddObjectToPool();
        }

        public static void DestroyPool<T>(T prefabComponent) where T: Component
        {
            DestroyPool(prefabComponent.gameObject);
        }

        public static void DestroyPool(GameObject prefab)
        {
            int instanceID = prefab.GetInstanceID();

            if (!prefabIDToPool.TryGetValue(instanceID, out Pool pool))
                return;

            pool.Destroy();
            prefabIDToPool.Remove(instanceID);
        }

        public static GameObject Spawn(GameObject prefab)
        {
            return Spawn(prefab, null, null, null).gameObject;
        }

        public static GameObject Spawn(GameObject prefab, Vector3 pos,
            Quaternion rot)
        {
            return Spawn(prefab, null, pos, rot).gameObject;
        }

        public static GameObject Spawn(GameObject prefab, Transform parent)
        {
            return Spawn(prefab, parent, null).gameObject;
        }

        public static T Spawn<T>(T prefab) where T : Component
        {
            return Spawn(prefab, null);
        }

        public static T Spawn<T>(T prefab, Vector3 pos, Quaternion rot) where T : Component
        {
            return Spawn(prefab, null, pos, rot);
        }

        public static T Spawn<T>(T prefab, Transform parent) where T : Component
        {
            return Spawn(prefab, parent, null);
        }

        public static T Spawn<T>(T prefab, Transform parent = null, Vector3? position = null, Quaternion? rotation =
            null) where T : Component
        {
            PoolMember poolMember = Spawn(prefab.gameObject, parent, position, rotation);
            T instance = poolMember.GetComponent<T>();
            return instance;
        }

        private static PoolMember Spawn(GameObject prefab, Transform parent = null, Vector3? position = null, Quaternion?
            rotation = null)
        {
            Pool pool = GetOrCreatePool(prefab);
            PoolMember poolMember = pool.Spawn(parent, position, rotation);
            return poolMember;
        }
        
        public static void DespawnAllMembers(GameObject root)
        {
            PoolMember[] childrens = root.GetComponentsInChildren<PoolMember>(true);

            for (int i = 0; i < childrens.Length; i++)
            {
                childrens[i].Despawn();
            }
        }

        public static void Despawn<T>(T prefab) where T : MonoBehaviour
        {
            Despawn(prefab.gameObject);
        }

        public static void Despawn(GameObject obj)
        {
            if (!instanceToPoolMember.TryGetValue(obj.GetInstanceID(), out PoolMember poolMember))
            {
#if DEBUG
                Debug.LogWarning("Object '" + obj.name + "' wasn't spawned from a pool. Destroying it instead.");
#endif
                Object.Destroy(obj);
                return;
            }
            poolMember.Despawn();
        }

        public static void Preload(GameObject prefab, int quantity)
        {
            Preload(prefab, quantity, null);
        }

        public static void Preload<T>(T component, Scene targetScene) where T: Component
        {
            Preload(component.gameObject, null, targetScene);
        }

        public static void Preload(GameObject prefab, Scene targetScene)
        {
            Preload(prefab, null, targetScene);
        }

        public static void Preload(GameObject prefab, Scene targetScene, bool allowDestroying = false)
        {
            Preload(prefab, null, targetScene, allowDestroying);
        }

        public static void Preload(GameObject prefab, Scene targetScene, int? quantity = null)
        {
            Preload(prefab, quantity, targetScene);
        }

        public static void Preload<T>(GameObject prefab, T component) where T: Component
        {
            Preload(prefab, null, component.gameObject.scene);
        }

        public static void Preload<T>(GameObject prefab, int quantity, T component) where T: Component
        {
            Preload(prefab, quantity, component.gameObject.scene);
        }

        public static void Preload<T>(GameObject prefab, Scene targetScene)
        {
            Preload(prefab, null, targetScene);
        }

        public static void Preload<T>(T component, int? quantity = null, Scene? targetScene = null, bool
            allowDestroying = false) where T : Component
        {
            Preload(component.gameObject, quantity, targetScene, allowDestroying);
        }
        
        public static void Preload(GameObject prefab, int? quantity = null, Scene? targetScene = null, bool
            allowDestroying = false)
        {
            Pool pool = GetOrCreatePool(prefab, quantity, targetScene, allowDestroying);

            for (int i = pool.TotalObjectCount; i < quantity; i++)
                pool.AddObjectToPool();
        }

        public static void RegisterPoolMember(PoolMember poolMember)
        {
            instanceToPoolMember.Add(poolMember.gameObject.GetInstanceID(), poolMember);
        }

        public static void UnregisterPoolMember(PoolMember poolMember)
        {
            instanceToPoolMember.Remove(poolMember.gameObject.GetInstanceID());
            
            if (poolMember.Pool != null)
                poolMember.Pool.UnregisterMember(poolMember);
        }

        public static void OnBeforeSceneUnload(Scene targetScene)
        {
            sceneUnloadCount++;
            if (!SimplePoolSettings.Instance.DespawnOnSceneUnload)
                return;

            foreach (var prefabToPool in prefabIDToPool)
                prefabToPool.Value.OnBeforeSceneUnload(targetScene);
        }

        public static bool BelongsToAPool<T>(T component, out PoolMember poolMember) where T: Component
        {
            return BelongsToAPool(component.gameObject, out poolMember);
        }

        public static bool BelongsToAPool(GameObject gameObject, out PoolMember poolMember)
        {
            return instanceToPoolMember.TryGetValue(gameObject.GetInstanceID(), out poolMember);
        }

        public static bool HasPoolForItem(GameObject targetGameObject)
        {
            return prefabIDToPool.TryGetValue(targetGameObject.GetInstanceID(), out _);
        }
        
        public static bool HasPoolForItem<T>(T targetComponent) where T: Component
        {
            return prefabIDToPool.TryGetValue(targetComponent.gameObject.GetInstanceID(), out _);
        }
    }
}
