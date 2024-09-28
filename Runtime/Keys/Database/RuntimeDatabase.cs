using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Vapor;
using Vapor.Inspector;
using Object = UnityEngine.Object;

namespace Vapor.Keys
{
    public static class RuntimeDatabaseUtility
    {
        public static void InitializeRuntimeDatabase(Type ofType, List<Object> keyValuePairs)
        {
            // Assuming T is known at runtime and is of type 'YourType'
            Type runtimeDatabaseGenericType = typeof(RuntimeDatabase<>);
            Type runtimeDatabaseType = runtimeDatabaseGenericType.MakeGenericType(ofType);

            // Find the method you want to call
            MethodInfo initKeyDatabaseMethod = runtimeDatabaseType.GetMethod("InitDatabase", BindingFlags.Public | BindingFlags.Static);

            if (initKeyDatabaseMethod != null)
            {
                // Make the method call
                initKeyDatabaseMethod.Invoke(null, new object[] { keyValuePairs });
            }
            else
            {
                Debug.LogError("[RuntimeDatabaseUtility] Method not found.");
            }
        }
    }

    public class RuntimeDatabase<T> where T : Object
    {
        private static Dictionary<int, T> s_Db;
        public static T Get(int id) => s_Db[id];
        public static bool TryGet(int id, out T value) => s_Db.TryGetValue(id, out value);
        public static IEnumerable<T> All() => s_Db.Values;

        public static void InitDatabase(List<Object> keyValuePairs)
        {

            s_Db ??= new Dictionary<int, T>();
            s_Db.Clear();

            if (typeof(T).GetInterfaces().Any(t => t == typeof(IKey)))
            {
                var converted = keyValuePairs.OfType<IKey>();
                foreach (var data in converted)
                {
                    if (data is IDatabaseInitialize dbInit)
                    {
                        dbInit.InitializedInDatabase();
                    }
                    s_Db.Add(data.Key, (T)data);
                }
            }
            else
            {
                foreach (var data in keyValuePairs)
                {
                    if (data is IDatabaseInitialize dbInit)
                    {
                        dbInit.InitializedInDatabase();
                    }
                    s_Db.Add(data.name.GetStableHashU16(), (T)data);
                }
            }
            Debug.Log($"{TooltipMarkup.ClassMethod(nameof(RuntimeDatabase<T>), nameof(InitDatabase))} - {TooltipMarkup.Class(typeof(T).Name)} - Loaded {s_Db.Count} Items");
        }

        public static void InitKeyDatabase<U>(KeyDatabaseSo<U> db) where U : ScriptableObject, IKey, T
        {
            Debug.Log($"RuntimeDatabase of type:{typeof(T).Name}. Init!");
            s_Db.Clear();
            foreach (var data in db.Data)
            {
                s_Db.Add(data.Key, data);
            }
        }

        public static void InitValueDatabase<U>(TypeDatabaseSo<U> db) where U : Object, T
        {
            Debug.Log($"RuntimeDatabase of type:{typeof(T).Name}. Init!");
            s_Db.Clear();

            foreach (var data in db.Data)
            {
                s_Db.Add(data.name.GetStableHashU16(), data);
            }
        }
    }
}
