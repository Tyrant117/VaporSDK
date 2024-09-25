using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Vapor.Keys;

namespace Vapor.GameplayTag
{
    public static class GameplayTagUtility
    {
        public const string CATEGORY_NAME = "GameplayTags";

        /// <summary>
        /// Gets a list of all keys defined in the GameplayTagKeys script if it exists in the project.
        /// </summary>
        /// <returns>Returns either the GameplayTagKeys.Values or a list with the "None" value if the GameplayTagKeys does not exist.</returns>
        public static List<(string, int)> GetAllGameplayTagsValues()
        {
            var kdvL = KeyUtility.GetAllKeysFromCategory(CATEGORY_NAME);
            List<(string, int)> result = new(kdvL.Count);
            foreach (var kvp in kdvL)
            {
                result.Add((kvp.Item1, kvp.Item2));
            }
            return result;           
        }

        /// <summary>
        /// Gets a list of all keys defined in the GameplayTagKeys script if it exists in the project.
        /// </summary>
        /// <returns>Returns either the GameplayTagKeys.DropdownValues or a list with the "None" value if the GameplayTagKeys does not exist.</returns>
        public static List<(string, KeyDropdownValue)> GetAllGameplayTagsKeyValues()
        {
            return KeyUtility.GetAllKeysFromCategory(CATEGORY_NAME);
        }
    }
}
