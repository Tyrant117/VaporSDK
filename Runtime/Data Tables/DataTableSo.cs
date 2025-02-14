using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Vapor.Blueprints;
using Vapor.Keys;

namespace Vapor.DataTables
{
    [CreateAssetMenu(fileName = "DataTableSo", menuName = "Scriptable Objects/DataTableSo")]
    [DatabaseKeyValuePair, KeyOptions(category: "DataTables")]
    public class DataTableSo : NamedKeySo, IDatabaseInitialize
    {
        [SerializeField]
        private SubclassOf<DataTableRowDefinition> _rowType;
        [SerializeReference, SerializeField]
        private List<DataTableRowDefinition> _runtimeData;
        public List<DataTableRowDefinition> RuntimeData => _runtimeData;

        private Dictionary<ushort, DataTableRowDefinition> _runtimeDataMap;

        public bool TryFindRow(string rowName, out DataTableRowDefinition rowData)
        {
            return TryFindRow(rowName.GetStableHashU16(), out rowData);
        }
        public bool TryFindRow(ushort rowHandle, out DataTableRowDefinition rowData)
        {
            return _runtimeDataMap.TryGetValue(rowHandle, out rowData);
        }
        
        public bool TryFindRow<T>(string rowName, out T rowData) where T : DataTableRowDefinition
        {
            return TryFindRow(rowName.GetStableHashU16(), out rowData);
        }
        public bool TryFindRow<T>(ushort rowHandle, out T rowData) where T : DataTableRowDefinition
        {
            if (_runtimeDataMap.TryGetValue(rowHandle, out var rd))
            {
                rowData = (T)rd;
                return true;
            }

            rowData = null;
            return false;
        }

        public Type GetRowType()
        {
            return _rowType.GetPinType();
        }

        public override void GenerateAdditionalKeys()
        {
#if UNITY_EDITOR
            var kvps = RuntimeData.Select(data => KeyGenerator.StringToKeyValuePair(data.RowHandle)).ToList();
            if (kvps.Count <= 0)
            {
                return;
            }

            string namespaceName = KeyGenerator.FindNearestAssemblyDefinition(this);
            namespaceName = namespaceName == null ? KeyGenerator.NamespaceName : $"{namespaceName}.{KeyGenerator.NamespaceName}";

            var path = KeyGenerator.ConvertFullPathToRelative(KeyGenerator.FindNearestDirectory(this));
            KeyGenerator.FormatKeyFiles(path, namespaceName, $"{name}Keys", $"{GetRowType().Name}RowHandles", kvps);
                
            RuntimeEditorUtility.SaveAndRefresh();
#endif
        }

        public void InitializedInDatabase()
        {
            _runtimeDataMap = new Dictionary<ushort, DataTableRowDefinition>(RuntimeData.Count);
            foreach (var data in RuntimeData)
            {
                _runtimeDataMap.Add(data.RowHandle.GetStableHashU16(), data);
            }
        }

        public void PostInitializedInDatabase()
        {
            
        }
    }
}
