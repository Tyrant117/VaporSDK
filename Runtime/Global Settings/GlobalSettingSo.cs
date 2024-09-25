using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vapor.Keys;
using Vapor.Observables;
using Vapor.SaveManager;

namespace Vapor.GlobalSettings
{
    [Serializable]
    internal struct GlobalSettingSaveDataContainer
    {
        public int Version { get; set; }
        public Dictionary<string, string> SaveDataPairs { get; set; }
    }

    internal class GlobalSettingSaveData : ISaveData
    {
        public const int VERSION = 1;
        public const string SERIALIZE_TO_APP_DATA = "__GlobalSetting_SerializeToAppData";

        public string Key => SERIALIZE_TO_APP_DATA;

        public string Filename => "GlobalSettingSaveData.json";

        public bool IsLoaded { get; protected set; }

        private Dictionary<string, string> _saveDataPairs;

        public GlobalSettingSaveData()
        {
            _saveDataPairs = new();
        }

        public object CaptureState()
        {
            Debug.Log("State captured!");
            return new GlobalSettingSaveDataContainer()
            {
                Version = VERSION,
                SaveDataPairs = _saveDataPairs
            };
        }

        public void RestoreState(object state)
        {
            var data = (GlobalSettingSaveDataContainer)state;
            _saveDataPairs = data.SaveDataPairs;
            Debug.Log($"[GlobalSettingSaveData] Restored State From Save: {_saveDataPairs.Count}");

            IsLoaded = true;
        }

        public string GetValueOrDefault(string key, string defaultValue)
        {
            Debug.Log($"[GlobalSettingSaveData] Getting Value At Key: {key} | default: {defaultValue}");
            return _saveDataPairs.TryGetValue(key, out string result) ? result : defaultValue;
        }

        public void SetValue(string key, string value)
        {
            _saveDataPairs[key] = value;
            _ = SaveManager.SaveManager.Save(Filename);
        }
    }

    [DatabaseKeyValuePair, KeyOptions(includeNone: false, category: GlobalSettingsConfig.CATEGORY_NAME)]
    public abstract class GlobalSettingSo : NamedKeySo
    {
        internal static GlobalSettingSaveData SaveData { get; set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Init()
        {
            SaveData = new GlobalSettingSaveData();
            SaveManager.SaveManager.RegisterSaveable(SaveData);
            _ = SaveManager.SaveManager.Load(SaveData.Filename);
        }

        protected static string GetSavePath(string name) => $"GlobalSetting_{name}";

        [SerializeField]
        protected GlobalSaveType SaveType;

        public string GetJsonStringValue(string defaultValue)
        {
            return SaveType switch
            {
                GlobalSaveType.PerSave => SaveData.IsLoaded ? SaveData.GetValueOrDefault(GetSavePath(DisplayName), defaultValue) : defaultValue,
                _ => PlayerPrefs.GetString(GetSavePath(DisplayName), defaultValue),
            };
        }

        public void SetJsonStringValue(string json)
        {
            switch (SaveType)
            {
                case GlobalSaveType.Global:
                default:
                    PlayerPrefs.SetString(GetSavePath(DisplayName), json);
                    break;
                case GlobalSaveType.PerSave:
                    SaveData.SetValue(GetSavePath(DisplayName), json);
                    break;
            }
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

        [NonSerialized]
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
                    _value.WithChanged(OnValueChanged);
                }
                return _value;
            }
        }

        private void OnValueChanged(Observable<T> observable, T t)
        {
            SetJsonStringValue(_value.SaveAsJson());
        }

        protected override object GetBoxedValue()
        {
            return Value.Value;
        }
    }
}
