﻿using System.Linq;
using UnityEngine;

namespace BrunoMikoski.Pooling
{
    public class ResourceScriptableObjectSingleton<T> : ScriptableObject where T: ScriptableObject
    {
        private static T instance;
        public static T Instance
        {
            get
            {
                if (instance == null)
                    instance = LoadOrCreateInstance();
                return instance;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static T LoadOrCreateInstance()
        {
            if (!TryToLoadInstance(out T resultInstance))
            {
#if !UNITY_EDITOR
                return null;
#else
                resultInstance = CreateInstance<T>();

                AssetDatabaseUtils.CreatePathIfDontExist("Assets/Resources");
                UnityEditor.AssetDatabase.CreateAsset(resultInstance, $"Assets/Resources/{typeof(T).Name}.asset");
                UnityEditor.AssetDatabase.SaveAssets();
                UnityEditor.AssetDatabase.Refresh();
                return resultInstance;
#endif         
            }

            return resultInstance;
        }

        public static bool Exist()
        {
            return TryToLoadInstance(out _);
        }

        private static bool TryToLoadInstance(out T result)
        {
            T newInstance = Resources.Load<T>(typeof(T).Name);

            if (newInstance != null)
            {
                result = newInstance;
                return true;
            }

#if UNITY_EDITOR
            string registryGUID = UnityEditor.AssetDatabase.FindAssets($"t:{typeof(T).Name}")
                .FirstOrDefault();

            if (!string.IsNullOrEmpty(registryGUID))
            {
                newInstance = (T) UnityEditor.AssetDatabase.LoadAssetAtPath<ScriptableObject>(
                    UnityEditor.AssetDatabase.GUIDToAssetPath(registryGUID));
            }

            if (newInstance != null)
            {
                result = newInstance;
                return true;
            }
#endif
            result = null;
            return false;
        }
        
    }
}
