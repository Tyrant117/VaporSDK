using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vapor.Inspector;

namespace Vapor.GlobalSettings
{
    //[CreateAssetMenu(fileName = "GlobalString", menuName = "Vapor/Global Settings/String", order = VaporConfig.GlobalSettingsPriority + 4)]
    public class GlobalSettingStringSo : GlobalSettingSo
    {
        [SerializeField]
        protected string StartingValue;

        [NonSerialized]
        private string _value = null;
        public string Value
        {
            get
            {
                if (_value.EmptyOrNull())
                {
                    var defaultVal = GetJsonStringValue(StartingValue);
                    _value = defaultVal;
                }
                return _value;
            }
            set
            {
                string old = _value;
                _value = value;
                ValueChanged?.Invoke(_value, old);
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
