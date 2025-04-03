using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Compilation;
using UnityEngine;
using Vapor.Inspector;
using Assembly = System.Reflection.Assembly;
#if UNITY_EDITOR
using UnityEditorInternal;
#endif

namespace Vapor.Blueprints
{
    [CreateAssetMenu(fileName = "AssemblyReferenceSo", menuName = "Scriptable Objects/AssemblyReferenceSo")]
    public class AssemblyReferenceSo : VaporScriptableObject
    {
#if UNITY_EDITOR
        // [OnValueChanged("ForceUpdateNamespaces", true)]
        public List<AssemblyDefinitionAsset> Assemblies;
        // [ValueDropdown("@GetAllNamespaceValues")]
        // public List<string> Namespaces;
        // public bool IncludeEditor;

        // public List<(string, string)> GetAllNamespaceValues()
        // {
        //     var types = GetTypesFromAssemblies(this);
        //     var choices = types.Where(t => !t.Namespace.EmptyOrNull()).Select(t => t.Namespace).Distinct().Select(ns => (ns, ns)).ToList();
        //     return choices;
        // }

        // private void ForceUpdateNamespaces()
        // {
        // }

        [Button]
        private void ListTypes()
        {
            var types = GetValidTypes(x => true);
            foreach (var t in types)
            {
                Debug.Log(t.FullName);
            }
        }

        public List<Type> GetValidTypes(Func<Type, bool> predicate)
        {
            var types = GetTypesFromAssemblies(this);
            return types.Where(predicate).ToList();
        }

        private static List<Type> GetTypesFromAssemblies(AssemblyReferenceSo assemblyReference)
        {
            var allTypes = new List<Type>();
            // Get all assemblies from AssemblyDefinitionAssets
            foreach (var asmDef in assemblyReference.Assemblies)
            {
                if (!asmDef)
                {
                    continue;
                }
                
                var compiledAssembly = CompilationPipeline.GetAssemblies().FirstOrDefault(a => a.name == asmDef.name);
                if (compiledAssembly == null)
                {
                    continue;
                }

                // if (!assemblyReference.IncludeEditor && compiledAssembly.flags.HasFlag(AssemblyFlags.EditorAssembly))
                // {
                //     continue;
                // }

                // Load the assembly
                var assembly = Assembly.Load(compiledAssembly.name);
                if (assembly == null)
                {
                    continue;
                }

                // Get all types from the assembly
                allTypes.AddRange(assembly.GetTypes().Where(t => (t.IsPublic || t.IsNestedPublic) && !t.IsAbstract));
            }

            return allTypes;
        }
#endif
    }
}
