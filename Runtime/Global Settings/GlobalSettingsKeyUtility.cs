using System.Collections.Generic;
using Vapor.Keys;

namespace Vapor.GlobalSettings
{
    public static class GlobalSettingsKeyUtility
    {
        public static List<(string, KeyDropdownValue)> GetAllGlobalSettingKeyValues()
        {
            return KeyUtility.GetAllKeysOfNamedType(GlobalSettingsConfig.KeyName);
        }
    }
}
