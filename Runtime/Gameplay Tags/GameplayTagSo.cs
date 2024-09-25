using System.Collections.Generic;
using UnityEngine;
using Vapor.Inspector;
using Vapor.Keys;

namespace Vapor.GameplayTag
{
    [DatabaseKeyValuePair, KeyOptions(useNameAsGuid: true, category: GameplayTagUtility.CATEGORY_NAME)]
    public class GameplayTagSo : NamedKeySo
    {
        [ValueDropdown("@GetAllTagValues", searchable: true)]
        public KeyDropdownValue Parent;

        public static List<(string, KeyDropdownValue)> GetAllTagValues()
        {
            return GameplayTagUtility.GetAllGameplayTagsKeyValues();
        }
    }
}
