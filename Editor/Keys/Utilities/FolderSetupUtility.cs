using UnityEditor;
using Vapor.InspectorEditor;

namespace VaporKeysEditor
{
    internal static class FolderSetupUtility
    {
        public const string EditorNamespace = "Vapor.KeysEditor";

        public const string FolderRelativePath = "Vapor/Keys";
        public const string DefinitionsRelativePath = FolderRelativePath + "/Definitions";
        public const string ConfigRelativePath = FolderRelativePath + "/Config";

        public const string KeyRootNamespace = "VaporKeyDefinitions";
        public const string InternalAssemblyReferenceName = "VaporSDK.Keys";

        [InitializeOnLoadMethod]
        private static void SetupFolders()
        {
            FolderUtility.CreateFolderFromPath($"Assets/{FolderRelativePath}");
            FolderUtility.CreateFolderFromPath($"Assets/{DefinitionsRelativePath}");
            FolderUtility.CreateFolderFromPath($"Assets/{ConfigRelativePath}");

            FolderUtility.CreateAssemblyDefinition($"Assets/{FolderRelativePath}", KeyRootNamespace, KeyRootNamespace, new[] { InternalAssemblyReferenceName }, false);
        }
    }
}
