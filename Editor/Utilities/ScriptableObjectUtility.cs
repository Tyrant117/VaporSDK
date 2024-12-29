using UnityEditor;
using UnityEngine;
using System;
using System.IO;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets;
using Vapor.Keys;
using System.Reflection;

namespace VaporEditor
{
    public class ScriptableObjectAddressableHandler : AssetPostprocessor
    {
        private const string k_Extension = ".asset";

        static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths, bool didDomainReload)
        {
            foreach (string str in importedAssets)
            {
                //Debug.Log("Reimported Asset: " + str);
                if(IsScriptableObjectAsset(str))
                {
                    var so = AssetDatabase.LoadAssetAtPath<ScriptableObject>(str);
                    var atr = so.GetType().GetCustomAttribute<DatabaseKeyValuePairAttribute>();
                    if(atr != null)
                    {
                        AddToAddressables(str, atr.AddressableLabel);
                    }
                }
            }

            for (int i = 0; i < movedAssets.Length; i++)
            {
                //Debug.Log("Moved Asset: " + movedAssets[i] + " from: " + movedFromAssetPaths[i]);
                if (IsScriptableObjectAsset(movedAssets[i]))
                {
                    var so = AssetDatabase.LoadAssetAtPath<ScriptableObject>(movedAssets[i]);
                    var atr = so.GetType().GetCustomAttribute<DatabaseKeyValuePairAttribute>();
                    if (atr != null)
                    {
                        AddToAddressables(movedAssets[i], atr.AddressableLabel);
                    }
                }
            }
        }

        // Helper method to check if the asset is a ScriptableObject
        private static bool IsScriptableObjectAsset(string assetPath)
        {
            string extension = Path.GetExtension(assetPath);
            return extension == k_Extension;  // ScriptableObject assets usually have ".asset" extension
        }

        public static void AddToAddressables(string assetPath, string withLabel = null)
        {
            // Get the Addressable Asset Settings object (it manages all addressables)
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings.DefaultGroup == null)
            {
                Debug.LogError("A default addressable group must be created before auto-marking an object addressable.");
                return;
            }

            // Find or create an Addressable Group (we'll use the default group if none exists)
            AddressableAssetGroup group = settings.DefaultGroup;

            // Add the asset to the Addressable Group
            string guid = AssetDatabase.AssetPathToGUID(assetPath);
            Debug.Log(guid);
            AddressableAssetEntry entry = settings.CreateOrMoveEntry(guid, group);
            Debug.Log(entry);

            // Optional: Set an address name (e.g., same name as the asset)
            entry.address = Path.GetFileNameWithoutExtension(assetPath);
            if (withLabel != null)
            {
                entry.SetLabel(withLabel, true, true);
            }

            // Save changes to addressables
            AssetDatabase.SaveAssets();
        }
    }


    public static class ScriptableObjectUtility
    {
        /// <summary>Create a scriptable object asset</summary>
        /// <typeparam name="T">The type of asset to create</typeparam>
        /// <param name="assetPath">The full path and filename of the asset to create</param>
        /// <returns>The newly-created asset</returns>
        public static T CreateAt<T>(string assetPath) where T : ScriptableObject
        {
            return CreateAt(typeof(T), assetPath) as T;
        }

        /// <summary>Create a scriptable object asset</summary>
        /// <param name="assetType">The type of asset to create</param>
        /// <param name="assetPath">The full path and filename of the asset to create</param>
        /// <returns>The newly-created asset</returns>
        public static ScriptableObject CreateAt(Type assetType, string assetPath)
        {
            ScriptableObject asset = ScriptableObject.CreateInstance(assetType);
            if (asset == null)
            {
                Debug.LogError("failed to create instance of " + assetType.Name + " at " + assetPath);
                return null;
            }
            AssetDatabase.CreateAsset(asset, assetPath);
            return asset;
        }

        /// <summary>Create a ScriptableObject asset</summary>
        /// <typeparam name="T">The type of asset to create</typeparam>
        /// <param name="prependFolderName">If true, prepend the selected asset folder name to the asset name</param>
        /// <param name="trimName">If true, remove instances of the "Asset", "Attributes", "Container" strings from the name</param>
        public static string Create<T>(bool prependFolderName = false, bool trimName = true, Action<ScriptableObject> processAsset = null) where T : ScriptableObject
        {
            string className = typeof(T).Name;
            string assetName = className;
            string folder = GetSelectedAssetFolder();

            if (trimName)
            {
                var standardNames = new string[] { "Asset", "Attributes", "Container" };
                for (int i = 0; i < standardNames.Length; ++i)
                    assetName = assetName.Replace(standardNames[i], "");
            }

            if (prependFolderName)
            {
                string folderName = Path.GetFileName(folder);
                assetName = (string.IsNullOrEmpty(assetName) ? folderName : string.Format("{0}_{1}", folderName, assetName));
            }

            return Create(typeof(T), assetName, folder, processAsset);
        }

        private static string Create(string className, string assetName, string folder, Action<ScriptableObject> processAsset)
        {
            var asset = ScriptableObject.CreateInstance(className);
            if (asset == null)
            {
                Debug.LogError("failed to create instance of " + className);
                return string.Empty;
            }

            asset.name = assetName ?? className;
            processAsset?.Invoke(asset);

            string assetPath = AssetDatabase.GenerateUniqueAssetPath($"{folder}/{asset.name}.asset");
            ProjectWindowUtil.CreateAsset(asset, assetPath);
            return assetPath;
        }

        private static string Create(Type classType, string assetName, string folder, Action<ScriptableObject> processAsset)
        {
            var asset = ScriptableObject.CreateInstance(classType);
            if (asset == null)
            {
                Debug.LogError("failed to create instance of " + classType);
                return string.Empty;
            }

            asset.name = assetName ?? classType.Name;
            processAsset?.Invoke(asset);

            string assetPath = AssetDatabase.GenerateUniqueAssetPath($"{folder}/{asset.name}.asset");
            ProjectWindowUtil.CreateAsset(asset, assetPath);
            return assetPath;
        }

        private static string GetSelectedAssetFolder()
        {
            if ((Selection.activeObject != null) && AssetDatabase.Contains(Selection.activeObject))
            {
                string assetPath = AssetDatabase.GetAssetPath(Selection.activeObject);
                string assetPathAbsolute = string.Format("{0}/{1}", Path.GetDirectoryName(Application.dataPath), assetPath);

                if (Directory.Exists(assetPathAbsolute))
                {
                    return assetPath;
                }
                else
                {
                    return Path.GetDirectoryName(assetPath);
                }
            }

            return "Assets";
        }

        public static void AddToAddressables(string assetPath, string withLabel = null)
        {
            // Get the Addressable Asset Settings object (it manages all addressables)
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings.DefaultGroup == null)
            {
                Debug.LogError("A default addressable group must be created before auto-marking an object addressable.");
                return;
            }

            // Find or create an Addressable Group (we'll use the default group if none exists)
            AddressableAssetGroup group = settings.DefaultGroup;

            // Add the asset to the Addressable Group
            string guid = AssetDatabase.AssetPathToGUID(assetPath);
            AddressableAssetEntry entry = settings.CreateOrMoveEntry(guid, group);

            // Optional: Set an address name (e.g., same name as the asset)
            entry.address = Path.GetFileNameWithoutExtension(assetPath);
            if (withLabel != null)
            {
                entry.SetLabel(withLabel, true, true);
            }

            // Save changes to addressables
            AssetDatabase.SaveAssets();
        }
    }
}
