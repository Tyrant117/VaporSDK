using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Vapor.GlobalSettings
{
    //[CreateAssetMenu(fileName = "GlobalString", menuName = "Vapor/Global Settings/String", order = VaporConfig.GlobalSettingsPriority + 4)]
    public class GlobalSettingStringSo : GlobalSettingSo
    {
        [SerializeField]
        protected string StartingValue;

        private string _value;
        public string Value
        {
            get
            {
                if (_value == null)
                {
                    var defaultVal = GetJsonStringValue(string.Empty);
                    _value = defaultVal;
                }
                return _value;
            }
            set
            {
                string old = _value;
                _value = value;
                ValueChanged?.Invoke(_value, old);
                Debug.Log("Global Set");
                SetJsonStringValue(_value);
            }
        }

        public event Action<string, string> ValueChanged; // New - Old

        protected override object GetBoxedValue()
        {
            return Value;
        }
    }
}
