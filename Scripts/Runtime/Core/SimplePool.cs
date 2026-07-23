using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;
#endif
using Object = UnityEngine.Object;

namespace BrunoMikoski.Pooling
{
    public static class SimplePool
    {
        private const int INITIAL_POOL_MEMBER_DICTIONARY_SIZE = 512;

        private static Transform parent;

        private static Dictionary<EntityId, Pool> prefabIDToPool = new Dictionary<EntityId, Pool>();
        public static Dictionary<EntityId, Pool> PrefabIDToPool => prefabIDToPool;

        private static readonly Dictionary<EntityId, PoolMember> instanceToPoolMember =
            new Dictionary<EntityId, PoolMember>(INITIAL_POOL_MEMBER_DICTIONARY_SIZE);

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

            prefabIDToPool = new Dictionary<EntityId, Pool>();
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

            if (prefabIDToPool.TryGetValue(prefab.GetEntityId(), out Pool pool))
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
            prefabIDToPool[prefab.GetEntityId()] = pool;
        }

        public static void UnregisterPool(Pool targetPool)
        {
            prefabIDToPool.Remove(targetPool.Prefab.GetEntityId());
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
            EntityId instanceID = prefab.GetEntityId();

            if (!prefabIDToPool.TryGetValue(instanceID, out Pool pool))
                return;

            pool.Destroy();
            prefabIDToPool.Remove(instanceID);
        }

        public static GameObject Spawn(GameObject prefab)
        {
            return SpawnGameObject(prefab, null, null, null);
        }

        public static GameObject Spawn(GameObject prefab, Vector3 pos,
            Quaternion rot)
        {
            return SpawnGameObject(prefab, null, pos, rot);
        }

        public static GameObject Spawn(GameObject prefab, Transform parent)
        {
            return SpawnGameObject(prefab, parent, null, null);
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
            GameObject instance = SpawnGameObject(prefab.gameObject, parent, position, rotation);
            return instance.GetComponent<T>();
        }

        private static GameObject SpawnGameObject(GameObject prefab, Transform parent = null, Vector3? position = null,
            Quaternion? rotation = null)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
                return SpawnEditMode(prefab, parent, position, rotation);
#endif
            Pool pool = GetOrCreatePool(prefab);
            return pool.Spawn(parent, position, rotation).gameObject;
        }

#if UNITY_EDITOR
        private static GameObject InstantiateForEditMode(GameObject prefab, Transform parent)
        {
            bool isPrefab = PrefabUtility.IsPartOfAnyPrefab(prefab);
            if (isPrefab)
                return (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);

            return Object.Instantiate(prefab, parent, false);
        }

        private static GameObject SpawnEditMode(GameObject prefab, Transform parent, Vector3? position,
            Quaternion? rotation)
        {
            GameObject instance = InstantiateForEditMode(prefab, parent);

            if (parent != null)
                instance.transform.SetParent(parent);

            if (position.HasValue && rotation.HasValue)
            {
                instance.transform.SetPositionAndRotation(position.Value, rotation.Value);
            }
            else
            {
                if (position.HasValue)
                    instance.transform.position = position.Value;

                if (rotation.HasValue)
                    instance.transform.rotation = rotation.Value;
            }

            instance.SetActive(true);
            return instance;
        }
#endif
        
        public static void DespawnAllMembers(GameObject root)
        {
            PoolMember[] childrens = root.GetComponentsInChildren<PoolMember>(true);

            for (int i = 0; i < childrens.Length; i++)
            {
                childrens[i].Despawn();
            }
        }

        public static void Despawn<T>(T instance) where T : MonoBehaviour
        {
            Despawn(instance.gameObject);
        }

        public static void Despawn(GameObject obj)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                Object.DestroyImmediate(obj);
                return;
            }
#endif
            if (!instanceToPoolMember.TryGetValue(obj.GetEntityId(), out PoolMember poolMember))
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
        
        public static Pool Preload(GameObject prefab, int? quantity = null, Scene? targetScene = null, bool
            allowDestroying = false)
        {
            Pool pool = GetOrCreatePool(prefab, quantity, targetScene, allowDestroying);

            for (int i = pool.TotalObjectCount; i < quantity; i++)
                pool.AddObjectToPool();

            return pool;
        }

        public static void RegisterPoolMember(PoolMember poolMember)
        {
            instanceToPoolMember.Add(poolMember.gameObject.GetEntityId(), poolMember);
        }

        public static void UnregisterPoolMember(PoolMember poolMember)
        {
            instanceToPoolMember.Remove(poolMember.gameObject.GetEntityId());
            
            if (poolMember.Pool != null)
                poolMember.Pool.UnregisterMember(poolMember);
        }

        public static void BeforeSceneUnload(Scene targetScene)
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
            return instanceToPoolMember.TryGetValue(gameObject.GetEntityId(), out poolMember);
        }

        
        public static bool HasPoolForItem(GameObject targetGameObject)
        {
            return prefabIDToPool.TryGetValue(targetGameObject.GetEntityId(), out _);
        }

        public static bool HasPoolForItem(GameObject targetGameObject, out Pool pool)
        {
            return prefabIDToPool.TryGetValue(targetGameObject.GetEntityId(), out pool);
        }
        
        public static bool HasPoolForItem<T>(T targetComponent) where T: Component
        {
            return prefabIDToPool.TryGetValue(targetComponent.gameObject.GetEntityId(), out _);
        }

        public static void EnsurePoolSize(GameObject targetGameObject, int targetPoolSize, Scene? targetScene)
        {
            Pool pool = GetOrCreatePool(targetGameObject, targetPoolSize, targetScene);
            pool.EnsurePoolSize(targetPoolSize);
        }

        public static void EnsurePoolSize(Component targetComponent, int targetPoolSize, Scene? targetScene)
        {
            EnsurePoolSize(targetComponent.gameObject, targetPoolSize, targetScene);
        }
    }
}
