using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Vapor.GlobalSettings
{
    public static class GlobalSettingsConfig
    {
        public const string CATEGORY_NAME = "GlobalSettings";
    }

    public enum GlobalSaveType
    {
        /// <summary>
        /// The values will be saved in player prefs for all saves to access
        /// </summary>
        Global,
        /// <summary>
        /// The values will be saved in the current directory the save manager is pointing to.
        /// </summary>
        PerSave,
    }
}
