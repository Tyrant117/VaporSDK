using System;
using System.Collections.Generic;
using UnityEngine;
using Vapor.Inspector;

namespace Vapor.VisualScripting
{
    public class SetTempVariableNode<T> : IImpureNode, IReturnNode<T>
    {
        public uint Id { get; }
        public IGraph Graph
        {
            get => _graph;
            set => _graph = (FunctionGraph)value;
        }
        public IImpureNode Next { get; set; }
        public string VariableName { get; }

        private FunctionGraph _graph;
        public readonly IReturnNode<T> A;
        private readonly string _aPort;
        private readonly int _aPortIndex;

        public SetTempVariableNode(string guid, string variableName, NodePortTuple a)
        {
            Id = guid.GetStableHashU32();
            VariableName = variableName;
            A = (IReturnNode<T>)a.Node;
            _aPort = a.PortName;
            _aPortIndex = a.Index;
        }

        public void Invoke(IGraphOwner owner)
        {
            _graph.SetTempData(VariableName, A.GetValue(owner, _aPortIndex));
            Next.Invoke(owner);
        }

        public object GetBoxedValue(IGraphOwner owner, int portIndex)
        {
            return GetValue(owner, portIndex);
        }

        public T GetValue(IGraphOwner owner, int portIndex)
        {
            return _graph.GetTempData<T>(VariableName);
        }        

        public void Traverse(Action<INode> callback)
        {
            A.Traverse(callback);
            callback(this);
            Next.Traverse(callback);
        }
    }

    [Serializable, SearchableNode("Data/Set Temp Variable", "function")]
    public class SetTempVariableNodeModel : NodeModel
    {
        private const string k_ValueIn = "valueIn";
        private const string k_ValueOut = "valueOut";

        public override bool HasInPort => true;
        public override bool HasOutPort => true;

        [Serializable]
        public class InspectorDrawer
        {
            [OnValueChanged("OnNameChanged", false)]
            public string VariableName;
            [TypeSelector("@GetTypes")]
            public string VariableType;

            public static IEnumerable<Type> GetTypes()
            {
                return new List<Type>()
                {
                    typeof(int),
                    typeof(double),
                    typeof(Vector2),
                    typeof(Vector3)
                };
            }

            public NodeModel Owner { get; set; }

            private void OnNameChanged(string old, string @new)
            {
                Owner.OnRenameNode();
            }
        }

        public InspectorDrawer Inspector;

        public override void BuildSlots()
        {
            base.BuildSlots();

            InSlots.TryAdd(k_ValueIn, new PortSlot(k_ValueIn, "Value", PortDirection.In, typeof(object)).WithIndex(1));

            OutSlots.TryAdd(k_ValueOut, new PortSlot(k_ValueOut, "Value", PortDirection.Out, typeof(object))
                .SetAllowMultiple()
                .SetIsOptional()
                .WithIndex(1));
        }

        public override INode Build(GraphModel graph)
        {
            if (NodeRef != null)
            {
                return NodeRef;
            }

            var sValue = InSlots[k_ValueIn];
            NodePortTuple valueIn = new(graph.Get(sValue.Reference).Build(graph), sValue.Reference.PortName, 0);

            Type typeParameter = Type.GetType(Inspector.VariableType);
            Type genericType = typeof(GetTempVariableNode<>).MakeGenericType(typeParameter);
            var refN = (IImpureNode)Activator.CreateInstance(genericType, Guid, Inspector.VariableName, valueIn);

            NodeRef = refN;

            var sNext = OutSlots[k_Out];
            NodePortTuple next = new(graph.Get(sNext.Reference).Build(graph), sNext.Reference.PortName, 0);
            refN.Next = (IImpureNode)next.Node;
            return NodeRef;
        }

        public override (string, Color) GetNodeName() => Inspector != null ? ($"Set {Inspector.VariableName}", s_DefaultTextColor) : ("Set Temp Variable", s_DefaultTextColor);

        public override object GraphSettingsInspector()
        {
            if (Inspector != null)
            {
                Inspector.Owner = this;
                return Inspector;
            }

            Inspector = new InspectorDrawer()
            {
                Owner = this
            };
            return Inspector;
        }
    }
}
