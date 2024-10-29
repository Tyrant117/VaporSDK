using UnityEditor;
using UnityEngine;
using System;
using System.IO;

namespace VaporEditor
{
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
    }
}
