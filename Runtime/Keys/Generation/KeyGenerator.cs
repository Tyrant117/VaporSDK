#if UNITY_EDITOR
using UnityEditor;
#endif
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using System;
using System.Linq;
using System.Reflection;
using Vapor;
using Object = UnityEngine.Object;

namespace Vapor.Keys
{
    /// <summary>
    /// A static class for generating custom enum classes from IKey or string values.
    /// </summary>
    public static class KeyGenerator
    {
        public const string AbsoluteConfigPath = "Assets/Vapor/Keys/Config";
        public const string RelativeConfigPath = "Vapor/Keys/Config";
        public const string AbsoluteKeyPath = "Assets/Vapor/Keys/Definitions";
        public const string RelativeKeyPath = "Vapor/Keys/Definitions";
        public const string NamespaceName = "VaporKeyDefinitions";              

        #region - Keys -
        /// <summary>
        /// A helper struct linking all the data of a key together
        /// </summary>
        public readonly struct KeyValuePair
        {
            public readonly string DisplayName;
            public readonly string VariableName;
            public readonly string Guid;
            public readonly int Key;

            public KeyValuePair(string name, int key, string guid)
            {
                DisplayName = name;
                VariableName = Regex.Replace(name, " ", "");
                Guid = guid;
                Key = key;
            }

            public KeyValuePair(IKey key)
            {
                DisplayName = key.DisplayName;
                VariableName = Regex.Replace(key.DisplayName, " ", "");
                key.ForceRefreshKey();
                Guid = string.Empty;
#if UNITY_EDITOR
                if(key is Object so)
                {
                    Guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(so));
                    EditorUtility.SetDirty(so);
                }
#endif
                Key = key.Key;
            }

            public string GetFormat(int placeholderIndex)
            {
                var vName = VariableName.Length > 0 ? VariableName : "Placeholder_" + placeholderIndex;
                return $"public const int {vName} = {Key};";
            }
        }

        /// <summary>
        /// Returns a <see cref="KeyValuePair"/> from any string.
        /// </summary>
        /// <param name="key">The string to use</param>
        /// <returns>The KeyValuePair generated from the string.</returns>
        public static KeyValuePair StringToKeyValuePair(string key)
        {
            return new KeyValuePair(key, key.GetStableHashU16(), string.Empty);
        }
        #endregion

#if UNITY_EDITOR

        #region  Modify Keys
        public static void GenerateKeys(Type typeFilter, string scriptName, bool includeNone)
        {
            var guids = AssetDatabase.FindAssets($"t:{typeFilter.Name}");
            HashSet<int> takenKeys = new();
            List<KeyValuePair> formattedKeys = new();

            if (includeNone)
            {
                takenKeys.Add(0);
                formattedKeys.Add(new KeyValuePair("None", 0, string.Empty));
            }

            var sb = new StringBuilder();
            foreach (var item in GetAllAssetsFromGUIDs<Object>(guids))
            {
                if (item == null) { sb.AppendLine("Item Was Null"); continue; }
                if (item is not IKey key) { sb.AppendLine($"Item Was Not IKey: ({item.GetType()}) at {AssetDatabase.GetAssetPath(item)}"); continue; }
                if (key.IsDeprecated) { sb.AppendLine($"Key Was Deprecated: ({key.DisplayName}) at {AssetDatabase.GetAssetPath(item)}"); continue; }
                if (!key.ValidKey()) { sb.AppendLine($"Key Was Invalid: ({key.DisplayName}) at {AssetDatabase.GetAssetPath(item)}"); continue; }

                key.ForceRefreshKey();
                if (takenKeys.Contains(key.Key))
                {
                    Debug.LogError($"Key Collision: {item.name}. Objects cannot share a name.");
                }
                else
                {
                    EditorUtility.SetDirty(item);
                    takenKeys.Add(key.Key);
                    var guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(item));
                    formattedKeys.Add(new KeyValuePair(item.name, key.Key, guid));
                }
            }

            Debug.Log(sb.ToString());
            FormatKeyFiles(RelativeKeyPath, NamespaceName, scriptName, formattedKeys);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        public static void GenerateKeys(string searchFilter, string scriptName, bool includeNone)
        {
            HashSet<int> takenKeys = new();
            List<KeyValuePair> formattedKeys = new();

            if(includeNone)
            {
               takenKeys.Add(0);
               formattedKeys.Add(new KeyValuePair("None", 0, string.Empty));     
            }

            List<string> guids = new();
            guids.AddRange(AssetDatabase.FindAssets(searchFilter));
            foreach (var guid in guids)
            {
                var refVal = AssetDatabase.LoadAssetAtPath<Object>(AssetDatabase.GUIDToAssetPath(guid));
                if (refVal == null) { continue; }
                if (refVal is IKey refKey && (refKey.IsDeprecated || !refKey.ValidKey())) { continue; }

                var key = refVal.name.GetStableHashU16();
                if (!takenKeys.Add(key))
                {
                    Debug.LogError($"Key Collision: {refVal.name}. Objects cannot share a name.");
                }
                else
                {
                    if (refVal != null)
                    {
                        var soGuid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(refVal));
                        formattedKeys.Add(new KeyValuePair(refVal.name, key, soGuid));
                    }
                    else
                    {
                        formattedKeys.Add(new KeyValuePair(refVal.name, key, string.Empty));
                    }
                }
            }

            FormatKeyFiles(RelativeKeyPath, NamespaceName, scriptName, formattedKeys);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        public static void GenerateKeys<T>(string scriptName, bool includeNone) where T : ScriptableObject, IKey
        {
            var typeFilter = typeof(T).Name;
            Debug.Log($"Generating Keys of Type: {typeFilter}");
            GenerateKeys<T>(AssetDatabase.FindAssets($"t:{typeFilter}"), scriptName, includeNone);
        }

        public static void GenerateKeys<T>(IEnumerable<string> guids, string scriptName, bool includeNone) where T : ScriptableObject, IKey
        {
            HashSet<int> takenKeys = new();
            List<KeyValuePair> formattedKeys = new();

            if(includeNone)
            {
               takenKeys.Add(0);
               formattedKeys.Add(new KeyValuePair("None", 0, string.Empty));     
            }

            foreach (var item in GetAllAssetsFromGUIDs<T>(guids))
            {
                if (item == null) { continue; }
                if (item.IsDeprecated) { continue; }
                if (!item.ValidKey()) { continue; }

                item.ForceRefreshKey();
                if (takenKeys.Contains(item.Key))
                {
                    Debug.LogError($"Key Collision: {item.name}. Objects cannot share a name.");
                }
                else
                {
                    EditorUtility.SetDirty(item);
                    takenKeys.Add(item.Key);
                    var guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(item));
                    formattedKeys.Add(new KeyValuePair(item.name, item.Key, guid));
                }
            }

            FormatKeyFiles(RelativeKeyPath, NamespaceName, scriptName, formattedKeys);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        /// <summary>
        /// Directly adds a key to a Type.
        /// </summary>
        /// <param name="type">The type to add this key to. This type must be an auto-generated enum class</param>
        /// <param name="keyToAdd">The key to add to the class</param>
        public static void AddKey(Type type, IKey keyToAdd)
        {
            var newKvp = new KeyValuePair(keyToAdd);
            InternalAddKey(type, newKvp);
        }
        
        /// <summary>
        /// Directly adds a key to a Type.
        /// </summary>
        /// <param name="type">The type to add this key to. This type must be an auto-generated enum class</param>
        /// <param name="keyToAdd">The key to add to the class</param>
        public static void AddKey(Type type, string keyToAdd)
        {
            var newKvp = StringToKeyValuePair(keyToAdd);
            InternalAddKey(type, newKvp);
        }

        private static void InternalAddKey(Type type, KeyValuePair newKvp)
        {
            var relativePath = (string)type.GetField("RELATIVE_PATH", BindingFlags.Public | BindingFlags.Static)?.GetValue(null);
            var values = (List<(string, KeyDropdownValue)>)type.GetField("DropdownValues", BindingFlags.Public | BindingFlags.Static)?.GetValue(null);
            if(values == null) return;
            
            List<KeyValuePair> keyValuePairs = new();
            foreach (var value in values)
            {
                if (value.Item2.Key == newKvp.Key)
                {
                    Debug.LogError($"Key Collision: {value.Item1}. Objects cannot share a name.");
                    return;
                }

                keyValuePairs.Add(new KeyValuePair(value.Item1, value.Item2.Key, string.Empty));
            }

            keyValuePairs.Add(newKvp);

            FormatKeyFiles(relativePath, type.Namespace, type.Name, keyValuePairs);
        }

        /// <summary>
        /// Directly add a group of keys to a Type.
        /// </summary>
        /// <param name="type">The type to add this key to. This type must be an auto-generated enum class</param>
        /// <param name="keysToAdd">The keys to add to the class</param>
        public static void AddKeys(Type type, IEnumerable<IKey> keysToAdd)
        {
            List<KeyValuePair> newKvps = new();
            foreach (var keyToAdd in keysToAdd)
            {
                newKvps.Add(new KeyValuePair(keyToAdd));
            }
            InternalAddKeys(type, newKvps);
        }

        /// <summary>
        /// Directly add a group of keys to a Type.
        /// </summary>
        /// <param name="type">The type to add this key to. This type must be an auto-generated enum class</param>
        /// <param name="keysToAdd">The keys to add to the class</param>
        public static void AddKeys(Type type, IEnumerable<string> keysToAdd)
        {
            List<KeyValuePair> newKvps = new();
            foreach (var keyToAdd in keysToAdd)
            {
                newKvps.Add(StringToKeyValuePair(keyToAdd));
            }

            InternalAddKeys(type, newKvps);
        }

        private static void InternalAddKeys(Type type, List<KeyValuePair> newKeyValuePairs)
        {
            var relativePath = (string)type.GetField("RELATIVE_PATH", BindingFlags.Public | BindingFlags.Static)?.GetValue(null);
            var values = (List<(string, KeyDropdownValue)>)type.GetField("DropdownValues", BindingFlags.Public | BindingFlags.Static)?.GetValue(null);
            if(values == null) return;
            
            List<KeyValuePair> keyValuePairs = new();
            foreach (var value in values)
            {
                if (newKeyValuePairs.Exists(_Match))
                {
                    Debug.LogError($"Key Collision: {value.Item1}. Objects cannot share a name.");
                    return;
                }
                keyValuePairs.Add(new(value.Item1, value.Item2.Key, string.Empty));
                continue;
                
                bool _Match(KeyValuePair x) => x.Key == value.Item2.Key;
            }
            foreach (var newKvp in newKeyValuePairs)
            {
                keyValuePairs.Add(newKvp);
            }

            FormatKeyFiles(relativePath, type.Namespace, type.Name, keyValuePairs);
        }
        
        /// <summary>
        /// Directly removes a key from a Type.
        /// </summary>
        /// <param name="type">The type to add this key to. This type must be an auto-generated enum class</param>
        /// <param name="keyToRemove">The key to remove from the class</param>
        public static void RemoveKey(Type type, IKey keyToRemove)
        {
            var removeKvp = new KeyValuePair(keyToRemove);
            InternalRemoveKey(type, removeKvp);
        }

        /// <summary>
        /// Directly removes a key from a Type.
        /// </summary>
        /// <param name="type">The type to add this key to. This type must be an auto-generated enum class</param>
        /// <param name="keyToRemove">The key to remove from the class</param>
        public static void RemoveKey(Type type, string keyToRemove)
        {
            var removeKvp = StringToKeyValuePair(keyToRemove);
            InternalRemoveKey(type, removeKvp);
        }

        private static void InternalRemoveKey(Type type, KeyValuePair keyToRemove)
        {
            var relativePath = (string)type.GetField("RELATIVE_PATH", BindingFlags.Public | BindingFlags.Static)?.GetValue(null);
            var values = (List<(string, KeyDropdownValue)>)type.GetField("DropdownValues", BindingFlags.Public | BindingFlags.Static)?.GetValue(null);
            if(values == null) return;
            
            List<KeyValuePair> kvps = new();
            foreach (var value in values)
            {
                if (value.Item2.Key != keyToRemove.Key)
                {
                    kvps.Add(new KeyValuePair(value.Item1, value.Item2.Key, value.Item2.Guid));
                }
            }

            FormatKeyFiles(relativePath, type.Namespace, type.Name, kvps);
        }
        
        /// <summary>
        /// Directly removes a group of keys from a Type.
        /// </summary>
        /// <param name="type">The type to add this key to. This type must be an auto-generated enum class</param>
        /// <param name="keysToRemove">The keys to remove from the class</param>
        public static void RemoveKeys(Type type, IEnumerable<IKey> keysToRemove)
        {
            List<KeyValuePair> removeKvps = new();
            foreach (var keyToAdd in keysToRemove)
            {
                removeKvps.Add(new(keyToAdd));
            }
            InternalRemoveKeys(type, removeKvps);
        }

        /// <summary>
        /// Directly removes a group of keys from a Type.
        /// </summary>
        /// <param name="type">The type to add this key to. This type must be an auto-generated enum class</param>
        /// <param name="keysToRemove">The keys to remove from the class</param>
        public static void RemoveKeys(Type type, IEnumerable<string> keysToRemove)
        {
            List<KeyValuePair> removeKvps = new();
            foreach (var keyToAdd in keysToRemove)
            {
                removeKvps.Add(StringToKeyValuePair(keyToAdd));
            }
            InternalRemoveKeys(type, removeKvps);
        }

        private static void InternalRemoveKeys(Type type, List<KeyValuePair> keyValuePairsToRemove)
        {
            var relativePath = (string)type.GetField("RELATIVE_PATH", BindingFlags.Public | BindingFlags.Static)?.GetValue(null);
            var values = (List<(string, KeyDropdownValue)>)type.GetField("DropdownValues", BindingFlags.Public | BindingFlags.Static)?.GetValue(null);
            if(values == null) return;
            
            var keyValuePairs = (from value in values where !keyValuePairsToRemove.Exists(x => x.Key == value.Item2.Key) select new KeyValuePair(value.Item1, value.Item2.Key, value.Item2.Guid)).ToList();

            FormatKeyFiles(relativePath, type.Namespace, type.Name, keyValuePairs);
        }

        /// <summary>
        /// Directly removes a group of keys from a Type. Checking if they are deprecated first.
        /// </summary>
        /// <param name="type">The type to add this key to. This type must be an auto-generated enum class</param>
        /// <param name="keysToRemove">The keys to remove from the class</param>
        public static void RemoveDeprecated(Type type, IEnumerable<IKey> keysToRemove)
        {
            var removeKeyValuePairs = (from keyToRemove in keysToRemove where keyToRemove.IsDeprecated select new KeyValuePair(keyToRemove)).ToList();
            InternalRemoveKeys(type, removeKeyValuePairs);
        }
        #endregion

        private static IEnumerable<T> GetAllAssetsFromGUIDs<T>(IEnumerable<string> guids) where T : Object
        {
            foreach (var guid in guids)
            {
                yield return AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid));
            }
        }
#endif

        #region Format Keys
        /// <summary>
        /// Formats a list of <see cref="KeyValuePair"/> into a custom enum class.
        /// </summary>
        /// <param name="gameDataFilepath">The relative path to the save folder excluding the Assets folder.</param>
        /// <param name="namespaceName">The namespace that the resulting class should be in</param>
        /// <param name="scriptName">The name of resulting class</param>
        /// <param name="keys">The keys to be used</param>
        public static void FormatKeyFiles(string gameDataFilepath, string namespaceName, string scriptName, List<KeyValuePair> keys)
        {
            var gameDataProjectFilePath = $"/{gameDataFilepath}/{scriptName}.cs";
            var filepath = Application.dataPath + gameDataProjectFilePath;

            StringBuilder sb = new();

            sb.Append("//\t* THIS SCRIPT IS AUTO-GENERATED *\n");
            sb.Append("using System;\n");
            sb.Append("using Vapor.Keys;\n");
            sb.Append("using System.Collections.Generic;\n\n");

            sb.Append($"namespace {namespaceName}\n");
            sb.Append("{\n");
            sb.Append($"\tpublic class {scriptName}\n");
            sb.Append("\t{\n");


            FormatFilePath(sb, $"{gameDataFilepath}");
            FormatAttributeName(sb, namespaceName,$"{scriptName}");

            FormatEnum(sb, keys);

            FormatDropDown(sb, keys);

            for (int i = 0; i < keys.Count; i++)
            {
                int pIndex = i;
                sb.Append("\t\t");
                sb.Append(keys[i].GetFormat(pIndex));
                sb.Append("\n");
            }

            FormatList(sb, keys);
            FormatLookup(sb);
            FormatGet(sb);

            sb.Append("\t}\n");
            sb.Append("}");

            System.IO.File.WriteAllText(filepath, sb.ToString());
        }

        private static void FormatFilePath(StringBuilder sb, string relativePath)
        {
            sb.Append($"\t\tpublic const string RELATIVE_PATH = \"{relativePath}\";\n");
        }

        private static void FormatAttributeName(StringBuilder sb, string namespaceName, string className)
        {
            sb.Append($"\t\tpublic const string AssemblyQualifiedClassName = \"{namespaceName}.{className}, {namespaceName}, version=1.0.0.0, Culture=neutral, PublicKeyToken=null\";\n");
            sb.Append($"\t\tpublic const string FieldName = \"%DropdownValues\";\n");
        }

        private static void FormatEnum(StringBuilder sb, List<KeyValuePair> keys)
        {
            sb.Append($"\t\tpublic enum Enum : int\n");
            sb.Append("\t\t{\n");
            for (int i = 0; i < keys.Count; i++)
            {
                sb.Append($"\t\t\t{keys[i].VariableName} = {keys[i].Key},\n");
            }
            sb.Append("\t\t}\n\n");

            if (keys.Count <= 32)
            {
                sb.Append($"\t\t[Flags]\n");
                sb.Append($"\t\tpublic enum AsFlags : int\n");
                sb.Append("\t\t{\n");
                sb.Append($"\t\t\tNone = 0,\n");
                int skipNone = 0;
                for (int i = 0; i < keys.Count; i++)
                {
                    if(keys[i].DisplayName == "None") { skipNone = 1; continue; }

                    int flagID = i - skipNone;
                    sb.Append($"\t\t\t{keys[i].VariableName} = 1 << {flagID},\n");
                }
                sb.Append($"\t\t\tEverything = ~0,\n");
                sb.Append("\t\t}\n\n");

                sb.Append($"\t\tpublic static Dictionary<int, int> FlagToValueMap = new()\n");
                sb.Append("\t\t{\n");
                skipNone = 0;
                for (int i = 0; i < keys.Count; i++)
                {
                    if(keys[i].DisplayName == "None") { skipNone = 1; continue; }

                    int flag = 1 << (i - skipNone);
                    sb.Append($"\t\t\t{{ {flag}, {keys[i].Key} }},\n");
                }
                sb.Append("\t\t};\n\n");
            }
        }

        private static void FormatDropDown(StringBuilder sb, List<KeyValuePair> keys)
        {
            sb.Append($"\t\tpublic static List<(string, KeyDropdownValue)> DropdownValues = new()\n");
            sb.Append("\t\t{\n");
            for (int i = 0; i < keys.Count; i++)
            {
                sb.Append($"\t\t\tnew (\"{keys[i].DisplayName}\", new (\"{keys[i].Guid}\", {keys[i].VariableName})),\n");
            }
            sb.Append("\t\t};\n");
        }

        private static void FormatList(StringBuilder sb, List<KeyValuePair> keys)
        {
            sb.Append("\n");
            sb.Append($"\t\tpublic static List<int> Values = new ()\n");
            sb.Append("\t\t{\n");
            for (int i = 0; i < keys.Count; i++)
            {
                sb.Append($"\t\t\t{{ {keys[i].VariableName} }},\n");
            }
            sb.Append("\t\t};\n");
        }

        private static void FormatLookup(StringBuilder sb)
        {
            sb.Append($"\t\tpublic static string Lookup(int id)\n");
            sb.Append("\t\t{\n");

            sb.Append($"\t\t\treturn DropdownValues.Find((x) => x.Item2.Key == id).Item1;\n");

            sb.Append("\t\t}\n");
        }

        private static void FormatGet(StringBuilder sb)
        {
            sb.Append($"\t\tpublic static KeyDropdownValue Get(int id)\n");
            sb.Append("\t\t{\n");

            sb.Append($"\t\t\treturn DropdownValues.Find((x) => x.Item2.Key == id).Item2;\n");

            sb.Append("\t\t}\n");
        }
        #endregion
    }
}
