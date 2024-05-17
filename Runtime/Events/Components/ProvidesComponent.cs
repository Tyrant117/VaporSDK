using System.Collections.Generic;
using UnityEngine;
using Vapor.Inspector;
using Vapor.Keys;

namespace Vapor.Events
{
    /// <summary>
    /// A <see cref="MonoBehaviour"/> that provides the linked <see cref="_component"/> to anything requesting it with provided <see cref="_key"/><br />
    /// Values should be defined in the inspector.
    /// </summary>
    public class ProvidesComponent : VaporBehaviour
    {
        [SerializeField, ValueDropdown("$GetAllProviderKeyValues", searchable: true), IgnoreCustomDrawer]
        private KeyDropdownValue _key;
        [SerializeField]
        private Component _component;

        private void OnEnable()
        {
            if (_key.IsNone) return;
            
            ProviderBus.Get<CachedProviderData<Component>>(_key).Subscribe(OnComponentRequested);
        }

        private void OnDisable()
        {
            if (_key.IsNone) return;
            
            ProviderBus.Get<CachedProviderData<Component>>(_key).Unsubscribe(OnComponentRequested);
        }

        private Component OnComponentRequested()
        {
            return _component;
        }

        public static List<(string, KeyDropdownValue)> GetAllProviderKeyValues()
        {
            return EventKeyUtility.GetAllProviderKeyValues();
        }
    }
}
