using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VaporInspector;
using VaporKeys;

namespace VaporEvents
{
    public class ProvidesTransform : VaporBehaviour
    {
        [SerializeField, ValueDropdown("$GetAllProviderKeyValues", searchable: true), IgnoreCustomDrawer]
        private KeyDropdownValue _key;
        [SerializeField]
        private Transform _transform;

        private void OnEnable()
        {
            if (_key.IsNone) return;

            ProviderBus.Get<CachedProviderData<Transform>>(_key).Subscribe(OnComponentRequested);
        }

        private void OnDisable()
        {
            if (_key.IsNone) return;

            ProviderBus.Get<CachedProviderData<Transform>>(_key).Unsubscribe(OnComponentRequested);
        }

        private Transform OnComponentRequested()
        {
            return _transform;
        }

        public static List<(string, KeyDropdownValue)> GetAllProviderKeyValues()
        {
            return EventKeyUtility.GetAllProviderKeyValues();
        }
    }
}
