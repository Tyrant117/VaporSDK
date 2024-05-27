using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Vapor.PackageManager
{
    [CreateAssetMenu(fileName = "PackageDependencies", menuName = "Vapor/Package Manager/Dependencies", order = 20000)]
    public class PackageDependenciesSo : ScriptableObject
    {
        public List<PackageDependency> Dependencies = new();
    }
}
