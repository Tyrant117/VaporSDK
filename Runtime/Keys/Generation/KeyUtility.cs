using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Vapor.Keys
{
    public static class KeyUtility
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Init()
        {
            s_CachedKeyTypes.Clear();
            s_CachedFieldInfos.Clear();
        }

        public const string s_AssemblyName = "VaporKeyDefinitions";
        private static readonly Dictionary<string, Type> s_CachedKeyTypes = new();
        private static readonly Dictionary<Type, FieldInfo> s_CachedFieldInfos = new();

        public static List<(string, KeyDropdownValue)> GetAllKeysOfNamedType(string nameOfType, string assemblyName = s_AssemblyName)
        {
            if (s_CachedKeyTypes.TryGetValue(nameOfType, out var type) && s_CachedFieldInfos.TryGetValue(type, out var fieldInfo))
            {
                return (List<(string, KeyDropdownValue)>)fieldInfo?.GetValue(null);
            }

            var assembly = Assembly.Load($"{assemblyName}");
            type = assembly.GetType($"{assemblyName}.{nameOfType}");
            if (type == null)
            {
                return new List<(string, KeyDropdownValue)>() { ("None", KeyDropdownValue.None) };
            }
            else
            {
                s_CachedKeyTypes.Add(nameOfType, type);
                var fi = type.GetField("DropdownValues", BindingFlags.Public | BindingFlags.Static);
                s_CachedFieldInfos.Add(type, fi);
                return (List<(string, KeyDropdownValue)>)fi?.GetValue(null);
            }
        }

        public static List<(string, KeyDropdownValue)> GetAllKeysOfInternalAndNamedType(string nameOfType, Type internalType, string assemblyName = s_AssemblyName)
        {
            var result = new List<(string, KeyDropdownValue)>();
            result.AddRange((List<(string, KeyDropdownValue)>)internalType.GetField("DropdownValues", BindingFlags.Public | BindingFlags.Static)?.GetValue(null));

            if (s_CachedKeyTypes.TryGetValue(nameOfType, out var type) && s_CachedFieldInfos.TryGetValue(type, out var fieldInfo))
            {
                result.AddRange((List<(string, KeyDropdownValue)>)fieldInfo?.GetValue(null));
                return result;
            }

            var assembly = Assembly.Load($"{assemblyName}");
            type = assembly.GetType($"{assemblyName}.{nameOfType}");
            if (type == null)
            {
                return result;
            }
            else
            {
                s_CachedKeyTypes.Add(nameOfType, type);
                var fi = type.GetField("DropdownValues", BindingFlags.Public | BindingFlags.Static);
                s_CachedFieldInfos.Add(type, fi);
                result.AddRange((List<(string, KeyDropdownValue)>)fi?.GetValue(null));
                return result;
            }
        }

        public static List<int> GetAllValuesOfNamedType(string nameOfType, string assemblyName = s_AssemblyName)
        {
            if (s_CachedKeyTypes.TryGetValue(nameOfType, out var type))
            {
                return (List<int>)type.GetField("Values", BindingFlags.Public | BindingFlags.Static)?.GetValue(null);
            }

            var assembly = Assembly.Load($"{assemblyName}");
            type = assembly.GetType($"{assemblyName}.{nameOfType}");
            if (type == null)
            {
                return new List<int>() { 0 };
            }
            s_CachedKeyTypes.Add(nameOfType, type);
            return (List<int>)type.GetField("Values", BindingFlags.Public | BindingFlags.Static)?.GetValue(null);
        }

        public static List<int> GetAllValuesOfInternalAndNamedType(string nameOfType, string assemblyName = s_AssemblyName, params Type[] internalTypes)
        {
            var result = new List<int>();
            foreach (var internalType in internalTypes)
            {
                result.AddRange((List<int>)internalType.GetField("Values", BindingFlags.Public | BindingFlags.Static)?.GetValue(null));
            }

            if (s_CachedKeyTypes.TryGetValue(nameOfType, out var type))
            {
                result.AddRange((List<int>)type.GetField("Values", BindingFlags.Public | BindingFlags.Static)?.GetValue(null));
                return result;
            }

            var assembly = Assembly.Load($"{assemblyName}");
            type = assembly.GetType($"{assemblyName}.{nameOfType}");
            if (type == null)
            {
                return result;
            }
            s_CachedKeyTypes.Add(nameOfType, type);
            result.AddRange((List<int>)type.GetField("Values", BindingFlags.Public | BindingFlags.Static)?.GetValue(null));
            return result;
        }
    }
}
