using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using Vapor.Inspector;
using Vapor.Keys;

namespace Vapor.DataTables
{
    [CreateAssetMenu(fileName = "DataTableSo", menuName = "Scriptable Objects/CompositeDataTableSo")]
    [DatabaseKeyValuePair, KeyOptions(category: "DataTables")]
    public class CompositeDataTableSo : NamedKeySo, IDatabaseInitialize
    {
        [ValueDropdown("DataTables", ValueDropdownAttribute.FilterType.Category)]
        public List<KeyDropdownValue> DataTables;
        
        public bool TryFindRow(string rowName, out DataTableRowDefinition rowData)
        {
            return TryFindRow(rowName.GetStableHashU16(), out rowData);
        }
        public bool TryFindRow(ushort rowHandle, out DataTableRowDefinition rowData)
        {
            foreach (var dataTable in DataTables)
            {
                Assert.IsTrue(dataTable != Key, $"CompositeDataTable [{name}] cannot contain the same key [{dataTable.DisplayName}] as this table. Will cause overflow.");
                if (RuntimeDatabase<DataTableSo>.Get(dataTable).TryFindRow(rowHandle, out rowData))
                {
                    return true;
                }
            }
            rowData = null;
            return false;
        }
        
        public bool TryFindRow<T>(string rowName, out T rowData) where T : DataTableRowDefinition
        {
            return TryFindRow(rowName.GetStableHashU16(), out rowData);
        }
        public bool TryFindRow<T>(ushort rowHandle, out T rowData) where T : DataTableRowDefinition
        {
            foreach (var dataTable in DataTables)
            {
                Assert.IsTrue(dataTable != Key, $"CompositeDataTable [{name}] cannot contain the same key [{dataTable.DisplayName}] as this table. Will cause overflow.");
                if (RuntimeDatabase<DataTableSo>.Get(dataTable).TryFindRow(rowHandle, out rowData))
                {
                    return true;
                }
            }
            rowData = null;
            return false;
        }
        
        public void InitializedInDatabase()
        {
            
        }
        public void PostInitializedInDatabase()
        {
            
        }
    }
}