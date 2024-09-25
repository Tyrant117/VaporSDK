using UnityEditor;
using UnityEngine;
using Vapor.GlobalSettings;
using VaporEditor;

public static class GlobalSettingsMenu
{
    [MenuItem("Assets/Create/Vapor/Global Settings/Bool", priority = VaporConfig.AssetMenuPriority, secondaryPriority = 15)]
    private static void CreateGlobalSettingBool()
    {
        ScriptableObjectUtility.Create<GlobalSettingBoolSo>();
    }

    [MenuItem("Assets/Create/Vapor/Global Settings/Int", priority = VaporConfig.AssetMenuPriority, secondaryPriority = 16)]
    private static void CreateGlobalSettingInt()
    {
        ScriptableObjectUtility.Create<GlobalSettingIntSo>();
    }

    [MenuItem("Assets/Create/Vapor/Global Settings/Float", priority = VaporConfig.AssetMenuPriority, secondaryPriority = 17)]
    private static void CreateGlobalSettingFloat()
    {
        ScriptableObjectUtility.Create<GlobalSettingFloatSo>();
    }

    [MenuItem("Assets/Create/Vapor/Global Settings/String", priority = VaporConfig.AssetMenuPriority, secondaryPriority = 18)]
    private static void CreateGlobalSettingString()
    {
        ScriptableObjectUtility.Create<GlobalSettingStringSo>();
    }

    [MenuItem("Assets/Create/Vapor/Global Settings/ULong", priority = VaporConfig.AssetMenuPriority, secondaryPriority = 19)]
    private static void CreateGlobalSettingULong()
    {
        ScriptableObjectUtility.Create<GlobalSettingULongSo>();
    }
}
