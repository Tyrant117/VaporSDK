using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Vapor.PackageManager
{
    [System.Serializable]
    public struct PackageDependency
    {
        public string GitUrl;
        public string Version;
        public List<string> Defines;

        public readonly string GetFullPath() => string.IsNullOrEmpty(Version) ? GitUrl : $"{GitUrl}#{Version}";
    }
}
