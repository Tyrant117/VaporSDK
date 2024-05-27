using Newtonsoft.Json.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;
using Vapor.PackageManager;

namespace VaporEditor.PackageManager
{
    [InitializeOnLoad]
    public class PackageDependencyResolver
    {
        private static ListRequest s_ListRequest;
        private static AddAndRemoveRequest s_AddRemoveRequest;
        private static bool s_ResolveMissing;
        private static List<string> s_PackagesToLoad;

        [MenuItem("Vapor/Package Manager/Force Resolve Dependencies", priority = 2000, secondaryPriority = 1)]
        private static void ForceResolveDependencies()
        {
            ResolveDependencies(true, new List<string>());
        }

        [MenuItem("Vapor/Package Manager/Find Missing Dependencies", priority = 2000, secondaryPriority = 2)]
        private static void FindMissingDependencies()
        {
            s_ResolveMissing = false;
            CheckMissingDependencies();
        }

        static PackageDependencyResolver()
        {
            if (!SessionState.GetBool(nameof(PackageDependencyResolver), false))
            {
                CheckMissingByPackage();
                //s_ResolveMissing = true;
                //CheckMissingDependencies();
                SessionState.SetBool(nameof(PackageDependencyResolver), true);
            }
        }

        #region - Find Missing -
        private static void CheckMissingDependencies()
        {
            List<string> collection = new();
            var guids = AssetDatabase.FindAssets($"t:{nameof(PackageDependenciesSo)}", new string[] { "Packages" });

            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var package = AssetDatabase.LoadAssetAtPath<PackageDependenciesSo>(path);

                foreach (var dependency in package.Dependencies)
                {
                    if (string.IsNullOrEmpty(dependency.GetFullPath()))
                        continue;

                    bool processOn = true;
                    if (dependency.Defines != null)
                    {
                        foreach (var define in dependency.Defines)
                        {
                            if (!HasPreprocessorDirective(define))
                            {
                                Debug.Log($"Skipping Package: {dependency.GetFullPath()} because {define} is not true.");
                                processOn = false;
                                break;
                            }
                        }
                    }

                    if (!processOn)
                        continue;

                    collection.Add(dependency.GetFullPath());
                }
            }

            if (collection.Count > 0)
            {
                s_PackagesToLoad = collection;
                s_ListRequest = Client.List(true, false);
                Debug.Log("Checking Missing Dependencies");

                EditorApplication.update += ProgressFind;
            }
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
                    foreach (var pi in s_ListRequest.Result)
                    {
                        var url = pi.packageId.Split('@', options: System.StringSplitOptions.RemoveEmptyEntries);
                        Debug.Log($"{pi.packageId} | {pi.source}");
                        if (url.Length > 1 && s_PackagesToLoad.Contains(url[1]))
                        {
                            //Debug.Log($"Found Package! {pi.name} : {pi.version}");
                            s_PackagesToLoad.Remove(url[1]);
                        }
                    }

                    foreach (var missing in s_PackagesToLoad)
                    {
                        Debug.Log($"Missing Package: {missing}");
                    }


                    if (s_ResolveMissing)
                    {
                        Debug.Log($"Resolving [{s_PackagesToLoad.Count}] Missing Packages");
                        s_ResolveMissing = false;
                        await Task.Delay(1000);
                        ResolveDependencies(false, s_PackagesToLoad.ToList());
                    }
                    else
                    {
                        s_PackagesToLoad = null;
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
                var guids = AssetDatabase.FindAssets($"t:{nameof(PackageDependenciesSo)}", new string[] { "Packages" });

                foreach (var guid in guids)
                {
                    var path = AssetDatabase.GUIDToAssetPath(guid);
                    var package = AssetDatabase.LoadAssetAtPath<PackageDependenciesSo>(path);

                    foreach (var dependency in package.Dependencies)
                    {
                        if (string.IsNullOrEmpty(dependency.GetFullPath()))
                            continue;

                        bool processOn = true;
                        if (dependency.Defines != null)
                        {
                            foreach (var define in dependency.Defines)
                            {
                                if (!HasPreprocessorDirective(define))
                                {
                                    Debug.Log($"Skipping Package: {dependency.GetFullPath()} because {define} is not true.");
                                    processOn = false;
                                    break;
                                }
                            }
                        }

                        if (!processOn)
                            continue;

                        if (force || !SessionState.GetBool(dependency.GetFullPath(), false))
                        {
                            collection.Add(dependency.GetFullPath());
                        }
                    }
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

        #region - Test -
        [MenuItem("Vapor/Package Manager/Find By Package", priority = 2000, secondaryPriority = 3)]
        private static void FindMissingByPackage()
        {
            CheckMissingByPackage();
        }

        private static void CheckMissingByPackage()
        {
            //List<string> collection = new();
            //var guids = AssetDatabase.FindAssets($"package t:TextAsset", new string[] { "Packages" });

            //foreach (var guid in guids)
            //{
            //    var path = AssetDatabase.GUIDToAssetPath(guid);
            //    var extension = System.IO.Path.GetExtension(path);
            //    if (extension != ".json")
            //        continue;

            //    var textAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(path);
            //    var package = JObject.Parse(textAsset.text);
            //    var dependencies = (JObject)package["dependencies"];
            //    if (dependencies == null)
            //        continue;

            //    foreach (var dependency in dependencies)
            //    {
            //        Debug.Log(dependency);
            //    }
            //}

            //if (collection.Count > 0)
            //{
            //    s_PackagesToLoad = collection;
            //    s_ListRequest = Client.List(true, false);
            //    Debug.Log("Checking Missing Dependencies");

            //    EditorApplication.update += ProgressFind;
            //}

            s_ListRequest = Client.List(true, false);
            Debug.Log("Checking Missing Dependencies");

            EditorApplication.update += ProgressFindByPackage;
        }

        private static async void ProgressFindByPackage()
        {
            // Check if the request is completed
            if (s_ListRequest.IsCompleted)
            {
                // Unregister the callback
                EditorApplication.update -= ProgressFindByPackage;

                // Check if there were any errors during installation
                if (s_ListRequest.Status == StatusCode.Success)
                {
                    List<string> installed = new();
                    List<(string, string)> filtered = new();
                    var regex = new Regex(@"^(git|git\+https|https|ssh|git\+ssh)://");
                    foreach (var pi in s_ListRequest.Result)
                    {
                        installed.Add(pi.name);
                        if (pi.source is PackageSource.Embedded or PackageSource.Git)
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
                                Debug.Log(url);
                                if (regex.IsMatch(url))
                                {
                                    Debug.Log($"Package To Resolve: {url}");
                                    filtered.Add((dependency.Key, url));
                                }
                            }
                        }
                    }

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
                        result.Add(f.Item2);
                    }

                    Debug.Log($"Resolving [{filtered.Count}] Missing Packages");
                    await Task.Delay(1000);
                    if (result.Count > 0)
                    {
                        ResolveDependencies(false, result);
                    }
                }
                else
                {
                    Debug.LogError($"Failed to search for packages: {s_ListRequest.Error.message}");
                }
            }
        }
        #endregion

        #region - Helpers -
        private static bool HasPreprocessorDirective(string directive)
        {
            // Get the current build target
            BuildTargetGroup buildTarget = EditorUserBuildSettings.selectedBuildTargetGroup;

            // Get the preprocessor defines for the current build target
            string defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(buildTarget);

            // Check if the directive exists in the defines
            return directive[0] == '!' ? !defines.Contains(directive[1..]) : defines.Contains(directive);
        }
        #endregion
    }
}
