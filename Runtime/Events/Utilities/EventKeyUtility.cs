using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Vapor;
using Vapor.Keys;

namespace Vapor.Events
{
    /// <summary>
    /// A static class for managing keys generated from <see cref="EventKeySo"/> and <see cref="ProviderKeySo"/>
    /// </summary>
    public static class EventKeyUtility
    {
        public const string EVENTS_CATEGORY_NAME = "EventKeys";
        public const string PROVIDERS_CATEGORY_NAME = "ProviderKeys";

        /// <summary>
        /// Gets a list of all <see cref="EventKeySo"/> in the project. Will return null if not in the UNITY_EDITOR
        /// </summary>
        /// <returns></returns>
        public static List<(string, EventKeySo)> GetAllEventKeys()
        {
            var allProviderKeys = RuntimeAssetDatabaseUtility.FindAssetsByType<EventKeySo>();
            return allProviderKeys.Select(so => (so.DisplayName, so)).ToList();
        }
        
        /// <summary>
        /// Gets a list of all event keys defined in the EventKeyKeys script if it exists in the project.
        /// </summary>
        /// <returns>Returns either the EventKeyKeys.DropdownValues or a list with the "None" value if the EventKeyKeys does not exist.</returns>
        public static List<(string, KeyDropdownValue)> GetAllEventKeyValues()
        {
            return KeyUtility.GetAllKeysFromCategory(EVENTS_CATEGORY_NAME);
        }

        /// <summary>
        /// Gets a list of all <see cref="ProviderKeySo"/> in the project. Will return null if not in the UNITY_EDITOR
        /// </summary>
        /// <returns></returns>
        public static List<(string, ProviderKeySo)> GetAllProviderKeys()
        {
            var allProviderKeys = RuntimeAssetDatabaseUtility.FindAssetsByType<ProviderKeySo>();
            return allProviderKeys.Select(so => (so.DisplayName, so)).ToList();
        }

        /// <summary>
        /// Gets a list of all provider keys defined in the ProviderKeyKeys script if it exists in the project.
        /// </summary>
        /// <returns>Returns either the ProviderKeyKeys.DropdownValues or a list with the "None" value if the ProviderKeyKeys does not exist.</returns>
        public static List<(string, KeyDropdownValue)> GetAllProviderKeyValues()
        {
            return KeyUtility.GetAllKeysFromCategory(PROVIDERS_CATEGORY_NAME);
        }        
    }
}
