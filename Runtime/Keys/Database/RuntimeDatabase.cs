using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Vapor;
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
                Console.WriteLine("Method not found.");
            }
        }
    }

    public class RuntimeDatabase<T> where T : Object
    {
        private static Dictionary<int, T> _db;
        public static T Get(int id) => _db[id];
        public static bool TryGet(int id, out T value) => _db.TryGetValue(id, out value);
        public static IEnumerable<T> All() => _db.Values;

        public static void InitDatabase(List<Object> keyValuePairs)
        {
            
            _db ??= new Dictionary<int, T>();
            _db.Clear();

            if (typeof(T).GetInterfaces().Any(t => t == typeof(IKey)))
            {
                var converted = keyValuePairs.OfType<IKey>();
                foreach (var data in converted)
                {
                    _db.Add(data.Key, (T)data);
                }
            }
            else
            {
                foreach (var data in keyValuePairs)
                {
                    _db.Add(data.name.GetStableHashU16(), (T)data);
                }
            }
            Debug.Log($"RuntimeDatabase of type:{typeof(T).Name}. Init! Added: {_db.Count} items!");
        }

        public static void InitKeyDatabase<U>(KeyDatabaseSo<U> db) where U : ScriptableObject, IKey, T
        {
            Debug.Log($"RuntimeDatabase of type:{typeof(T).Name}. Init!");
            _db.Clear();
            foreach (var data in db.Data)
            {
                _db.Add(data.Key, data);
            }
        }

        public static void InitValueDatabase<U>(TypeDatabaseSo<U> db) where U : Object, T
        {
            Debug.Log($"RuntimeDatabase of type:{typeof(T).Name}. Init!");
            _db.Clear();

            foreach (var data in db.Data)
            {
                _db.Add(data.name.GetStableHashU16(), data);
            }
        }
    }
}
