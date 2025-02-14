using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using Vapor.Inspector;

namespace Vapor.DataTables
{
    [Serializable]
    public abstract class DataTableRowDefinition
    {
        [SerializeField, OnValueChanged("", false, delayed: true)] 
        protected string Handle;
        public string RowHandle
        {
            get => Handle;
            set => Handle = value;
        }

        public abstract IEnumerable<string> PrintColumnData();
    }

    [Serializable]
    public class RowTest : DataTableRowDefinition
    {
        [SerializeField] private int _intData;
        [SerializeField] private float _floatData;
        [SerializeField] private GameObject _gameObjectData;
        [SerializeField] private Vector3 _vector3Data;
        
        public override IEnumerable<string> PrintColumnData()
        {
            return new[]
            {
                _intData.ToString(),
                _floatData.ToString(CultureInfo.InvariantCulture),
                _gameObjectData.OrNull()?.ToString(),
                _vector3Data.ToString(),
            };
        }
    }
    
    [Serializable]
    public class ListOfStringsTest : DataTableRowDefinition
    {
        [SerializeField] private List<string> _stringData;
        
        public override IEnumerable<string> PrintColumnData()
        {
            return new[]
            {
                _stringData?.ToString(),
            };
        }
    }
}
