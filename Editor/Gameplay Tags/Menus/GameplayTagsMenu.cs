using UnityEditor;
using Vapor.GameplayTag;

namespace VaporEditor.GameplayTags
{
    public static class GameplayTagsMenu
    {
        [MenuItem("Assets/Create/Vapor/Gameplay Tags/Tag", priority = VaporConfig.AssetMenuPriority, secondaryPriority = 29)]
        private static void CreateNamedKey()
        {
            ScriptableObjectUtility.Create<GameplayTagSo>();
        }
    }
}
