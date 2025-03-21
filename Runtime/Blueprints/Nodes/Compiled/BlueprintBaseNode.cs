﻿using System;
using System.Collections.Generic;

namespace Vapor.Blueprints
{
    [Serializable]
    public struct BlueprintCompiledNodeDto
    {
        public Type NodeType;
        public string Guid;
        public List<BlueprintWireReference> InputWires;
        public Dictionary<string, (Type, object)> InputPinValues;
        public List<string> OutputPinNames;
        public Dictionary<string, object> Properties;
    }

    public abstract class BlueprintBaseNode
    {
        public const string NEXT_NODE_GUID = "NextNodeGuid";
        
        protected IBlueprintGraph Graph { get; set; }
        public string Guid { get; protected set; }
        protected List<BlueprintWireReference> InEdges { get; set; }
        
        protected Dictionary<string, object> InPortValues;
        protected Dictionary<string, object> OutPortValues;

        public abstract void Init(IBlueprintGraph graph);

        public void Invoke()
        {
            CacheInputValues();
            WriteOutputValues();
        }

        public virtual void InvokeAndContinue()
        {
            Invoke();
            Continue();
        }
        protected abstract void CacheInputValues();
        protected abstract void WriteOutputValues();
        protected abstract void Continue();

        public virtual bool TryGetOutputValue(string outPortName, out object outputValue)
        {
            return OutPortValues.TryGetValue(outPortName, out outputValue);
        }
    }
}