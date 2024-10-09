using System;
using UnityEngine;
using Vapor.Inspector;

namespace Vapor.Keys
{
    /// <summary>
    /// A scriptable object implementation of the IKey interface that derives its display name from the <see cref="ScriptableObject.name"/>
    /// Has a value that must be set in the editor.
    /// </summary>
    public class IntegerKeySo : VaporScriptableObject, IKey
    {
        [BoxGroup("Key", "Key Data"), SerializeField, ReadOnly, RichTextTooltip("The unique for this object.")]
        [InlineToggleButton("ToggleDeprecated", "@Deprecated", "d_VisibilityOff", "d_VisibilityOn", tooltip: "If <lw>Shut</lw>, this key will be ignored by KeyGenerator.GenerateKeys().")]
        [InlineButton("GenerateKeys", icon: "d_Refresh", tooltip: "Forces Generation of the keys for this Type")]
        private int _key;
        [SerializeField, HideInInspector]
        protected bool Deprecated;

        public int Key => _key;
        public string DisplayName => name;
        public bool IsDeprecated => Deprecated;

        public virtual bool ValidKey() { return true; }

        public void ForceRefreshKey() { }
#pragma warning disable IDE0051 // Remove unused private members
        private void ToggleDeprecated() { Deprecated = !Deprecated; }
#pragma warning restore IDE0051 // Remove unused private members        

        public void GenerateKeys()
        {
            var type = GetKeyScriptType();
            var scriptName = type.Name;
            scriptName = scriptName.Replace("Scriptable", "");
            scriptName = scriptName.Replace("SO", "");
            scriptName = scriptName.Replace("So", "");
            scriptName = scriptName.Replace("Key", "");
            KeyGenerator.GenerateKeys(type, $"{scriptName}Keys");
            GenerateAdditionalKeys();
        }

        public virtual Type GetKeyScriptType()
        {
            return GetType();
        }

        public virtual void GenerateAdditionalKeys() { }        
    }
}
