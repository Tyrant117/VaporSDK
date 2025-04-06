using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

namespace Vapor.Blueprints
{
    [Serializable]
    public struct BlueprintDesignNodeDto
    {
        public string Guid;
        public uint Uuid;
        public Type NodeType;
        public NodeType NodeEnumType;
        public Rect Position;
        public List<BlueprintWireReference> InputWires;
        public List<BlueprintWireReference> OutputWires;
        public List<BlueprintPinDto> InputPins;
        public List<BlueprintPinDto> OutputPins;
        public Dictionary<string, (Type, object)> Properties;
        
        public T GetProperty<T>(string propertyName)
        {
            Assert.IsTrue(Properties.ContainsKey(propertyName), $"Property {propertyName} was not found");
            var tuple = Properties[propertyName];
            return (T)TypeUtility.CastToType(tuple.Item2, tuple.Item1);
        }

        public static BlueprintDesignNodeDto New(NodeType nodeEnumType, Vector2 position)
        {
            var guid = System.Guid.NewGuid().ToString();
            return new BlueprintDesignNodeDto
            {
                Guid = guid,
                Uuid = guid.GetStableHashU32(),
                NodeEnumType = nodeEnumType,
                Position = new Rect(position, Vector2.zero),
                InputWires = new List<BlueprintWireReference>(),
                OutputWires = new List<BlueprintWireReference>(),
            };
        }

        public BlueprintDesignNodeDto WithProperty<T>(string propertyName, T propertyValue)
        {
            Properties[propertyName] = (typeof(T), propertyValue);
            return this;
        }
    }
}