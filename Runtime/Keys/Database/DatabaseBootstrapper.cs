using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Vapor.Keys
{
    public static class DatabaseBootstrapper
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Init()
        {
            if (Application.isEditor)
            {
#if UNITY_EDITOR
                var types = TypeCache.GetTypesWithAttribute(typeof(DatabaseKeyValuePairAttribute));
                foreach (var type in types)
                {
                    var assets = RuntimeAssetDatabaseUtility.FindAssetsByType(type);
                    RuntimeDatabaseUtility.InitializeRuntimeDatabase(type, assets);
                }

                foreach (var type in types)
                {
                    RuntimeDatabaseUtility.PostInitializeRuntimeDatabase(type);
                }
#endif
            }
            else
            {
                // Get all loaded assemblies in the current application domain
                Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();

                // Iterate through each assembly
                foreach (Assembly assembly in assemblies)
                {
                    // Get all types in the assembly
                    Type[] types = assembly.GetTypes();

                    // Iterate through each type
                    foreach (Type type in types)
                    {
                        // Check if the type has the DatabaseKeyValuePair attribute
                        if (type.IsDefined(typeof(DatabaseKeyValuePairAttribute), false))
                        {
                            var assets = Resources.LoadAll("", type);
                            RuntimeDatabaseUtility.InitializeRuntimeDatabase(type, assets.ToList());
                        }
                    }

                    foreach (var type in types)
                    {
                        if (type.IsDefined(typeof(DatabaseKeyValuePairAttribute), false))
                        {
                            RuntimeDatabaseUtility.PostInitializeRuntimeDatabase(type);
                        }
                    }
                }
            }
        }
    }
}
