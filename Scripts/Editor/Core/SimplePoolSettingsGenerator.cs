using UnityEditor;
using UnityEngine;

namespace BrunoMikoski.Pooling
{
    [InitializeOnLoad]
    public static class SimplePoolSettingsGenerator
    {
        static SimplePoolSettingsGenerator()
        {
            if (Application.isPlaying)
                return;

            SimplePoolSettings.LoadOrCreateInstance();
        }
    }
}
