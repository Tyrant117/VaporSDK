using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vapor.Keys;
using Vapor.Observables;

namespace Vapor.GlobalSettings
{
    [DatabaseKeyValuePair]
    public abstract class GlobalSettingSo : NamedKeySo
    {
        protected static string GetSavePath(string name) => $"GlobalSetting_{name}";

        public string GetJsonStringValue(string defaultValue)
        {
            return PlayerPrefs.GetString(GetSavePath(DisplayName), defaultValue);
        }

        public void SetJsonStringValue(string json)
        {
            PlayerPrefs.SetString(GetSavePath(DisplayName), json);
        }

        public override Type GetKeyScriptType()
        {
            return typeof(GlobalSettingSo);
        }

        protected abstract object GetBoxedValue();

        public bool TryGetValue<T>(out T value) where T : struct
        {
            var boxed = GetBoxedValue();
            value = (T)boxed;
            return true;
        }
    }

    public abstract class GlobalSettingSo<T> : GlobalSettingSo where T : struct
    {
        [SerializeField]
        protected T StartingValue;

        private Observable<T> _value;
        public Observable<T> Value
        {
            get
            {
                if (_value == null)
                {
                    var wrapped = new Observable<T>(DisplayName, true, StartingValue);
                    var defaultVal = GetJsonStringValue(wrapped.SaveAsJson());
                    var deserialized = (Observable<T>)Observable.Load(defaultVal);
                    _value = deserialized;
                }
                return _value;
            }
            set
            {
                _value = value;
                Debug.Log("Global Set");
                SetJsonStringValue(_value.SaveAsJson());
            }
        }

        protected override object GetBoxedValue()
        {
            return Value.Value;
        }
    }
}
