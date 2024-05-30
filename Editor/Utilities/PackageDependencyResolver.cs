using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;
using Vapor;

namespace VaporEditor
{
    [InitializeOnLoad]
    public class PackageDependencyResolver
    {
        private static ListRequest s_ListRequest;
        private static AddAndRemoveRequest s_AddRemoveRequest;
        private static bool s_ResolveMissing;
        private static List<string> s_PackagesToLoad;

        [MenuItem("Assets/Create/Vapor/Package Manager/Create Dependency File", priority = VaporConfig.AssetMenuPriority, secondaryPriority = 1030)]
        private static void CreateDependencyFile()
        {
            string json =
@"{
 ""dependencies"": {
 }
}";
            ProjectWindowUtil.CreateAssetWithContent("dependencies.json", json, (Texture2D)EditorGUIUtility.IconContent("d_TextAsset Icon").image);
        }

        [MenuItem("Vapor/Package Manager/Force Resolve Dependencies", priority = 2000, secondaryPriority = 1)]
        private static void ForceResolveDependencies()
        {
            ResolveDependencies(true, new List<string>());
        }

        [MenuItem("Vapor/Package Manager/Find Missing Dependencies", priority = 2000, secondaryPriority = 2)]
        private static void FindMissingDependencies()
        {
            s_ResolveMissing = false;
            CheckingMissingDependencies();
        }

        static PackageDependencyResolver()
        {
            if (!SessionState.GetBool(nameof(PackageDependencyResolver), false))
            {
                s_ResolveMissing = true;
                CheckingMissingDependencies();
                SessionState.SetBool(nameof(PackageDependencyResolver), true);
            }
        }

        #region - Find Missing -
        private static void CheckingMissingDependencies()
        {
            s_ListRequest = Client.List(true, false);
            Debug.Log("Checking Missing Dependencies");

            EditorApplication.update += ProgressFind;
        }

        private static async void ProgressFind()
        {
            // Check if the request is completed
            if (s_ListRequest.IsCompleted)
            {
                // Unregister the callback
                EditorApplication.update -= ProgressFind;

                // Check if there were any errors during installation
                if (s_ListRequest.Status == StatusCode.Success)
                {
                    FilterDependencies(out List<string> installed, out List<(string, string)> filtered);

                    foreach (var i in installed)
                    {
                        var idx = filtered.FindIndex(x => x.Item1 == i);
                        if (idx != -1)
                        {
                            filtered.RemoveAt(idx);
                        }
                    }

                    List<string> result = new();
                    foreach (var f in filtered)
                    {
                        Debug.Log($"Found Missing Package: {f.Item2}");
                        result.Add(f.Item2);
                    }

                    if (result.Count == 0)
                        Debug.Log("All Packages Found!");

                    if (s_ResolveMissing)
                    {
                        s_ResolveMissing = false;
                        await Task.Delay(1000);
                        if (result.Count > 0)
                        {
                            Debug.Log($"Resolving [{result.Count}] Missing Packages");
                            ResolveDependencies(false, result);
                        }
                    }
                }
                else
                {
                    Debug.LogError($"Failed to search for packages: {s_ListRequest.Error.message}");
                }
            }
        }
        #endregion

        #region - Resolve Dependencies -
        private static void ResolveDependencies(bool force, List<string> collection)
        {
            if (collection.Count == 0)
            {
                FilterDependencies(out List<string> installed, out List<(string, string)> filtered);

                if (!force)
                {
                    foreach (var i in installed)
                    {
                        var idx = filtered.FindIndex(x => x.Item1 == i);
                        if (idx != -1)
                        {
                            filtered.RemoveAt(idx);
                        }
                    }
                }

                foreach (var f in filtered)
                {
                    Debug.Log($"Found Missing Package: {f.Item2}");
                    collection.Add(f.Item2);
                }
            }

            if (collection.Count > 0)
            {
                s_PackagesToLoad = collection;
                s_AddRemoveRequest = Client.AddAndRemove(collection.ToArray());
                Debug.Log("Generating Dependancy Graph");

                // Register the callback to handle the request completion
                EditorApplication.update += ProgressResolve;
            }
        }

        private static void ProgressResolve()
        {
            // Check if the request is completed
            if (s_AddRemoveRequest.IsCompleted)
            {                
                // Unregister the callback
                EditorApplication.update -= ProgressResolve;

                // Check if there were any errors during installation
                if (s_AddRemoveRequest.Status == StatusCode.Success)
                {
                    Debug.Log("Package dependencies installed successfully!");
                    foreach (var pi in s_AddRemoveRequest.Result)
                    {
                        var url = pi.packageId.Split('@', options: System.StringSplitOptions.RemoveEmptyEntries);                        
                        if (url.Length > 1 && s_PackagesToLoad.Contains(url[1]))
                        {
                            s_PackagesToLoad.Remove(url[1]);
                            SessionState.SetBool(url[1], true);
                            Debug.Log($"Installed: {pi.displayName} from {url[1]}");
                        }
                    }
                }
                else
                {
                    Debug.LogError($"Failed to install dependencies package: {s_AddRemoveRequest.Error.message}");
                }
                s_PackagesToLoad = null;
            }
        }
        #endregion

        #region - Helpers -
        private static void FilterDependencies(out List<string> installed, out List<(string, string)> filtered)
        {
            installed = new();
            filtered = new();
            var regex = new Regex(@"^(git|git\+https|https|ssh|git\+ssh)://");
            foreach (var pi in s_ListRequest.Result)
            {
                installed.Add(pi.name);
                if (pi.source is PackageSource.Git)
                {
                    var textAsset = AssetDatabase.LoadAssetAtPath<TextAsset>($"{pi.assetPath}/dependencies.json");
                    if (textAsset == null)
                    {
                        Debug.Log($"File Not Found: {pi.assetPath}/dependencies.json");
                        continue;
                    }

                    var package = JObject.Parse(textAsset.text);
                    var dependencies = (JObject)package["dependencies"];
                    if (dependencies == null)
                    {
                        Debug.Log($"Dependecies Not Found: Create a \"dependencies\"{{}} section in the Json File.");
                        continue;
                    }

                    foreach (var dependency in dependencies)
                    {
                        var url = dependency.Value.Value<string>();
                        if (regex.IsMatch(url))
                        {
                            filtered.Add((dependency.Key, url));
                        }
                    }
                }
            }
        }
        #endregion
    }
}
