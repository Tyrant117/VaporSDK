using System;
using System.Collections.Generic;
using UnityEngine;
using Vapor.Inspector;

namespace Vapor.VisualScripting
{
    public class GetTempVariableNode<T> : IReturnNode<T>
    {
        public uint Id { get; }
        public IGraph Graph
        {
            get => _graph;
            set => _graph = (FunctionGraph)value;
        }
        public string VariableName { get; }

        private FunctionGraph _graph;

        public GetTempVariableNode(string guid, string variableName)
        {
            Id = guid.GetStableHashU32();
            VariableName = variableName;
        }

        public T GetValue(IGraphOwner owner, string portName = "")
        {
            return _graph.GetTempData<T>(VariableName);
        }

        public void Traverse(Action<INode> callback)
        {
            callback(this);
        }
    }

    [Serializable, SearchableNode("Data/Get Temp Variable", "function")]
    public class GetTempVariableNodeModel : NodeModel
    {
        private const string k_ValueOut = "valueOut";

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

            OutSlots.TryAdd(k_ValueOut, new PortSlot(k_ValueOut, "Value", PortDirection.Out, typeof(object))
                .SetAllowMultiple()
                .SetIsOptional());
        }

        public override INode Build(GraphModel graph)
        {
            if (NodeRef != null)
            {
                return NodeRef;
            }

            Type typeParameter = Type.GetType(Inspector.VariableType);
            Type genericType = typeof(GetTempVariableNode<>).MakeGenericType(typeParameter);
            var refN = (INode)Activator.CreateInstance(genericType, Guid, Inspector.VariableName);

            NodeRef = refN;
            return NodeRef;
        }

        public override (string, Color) GetNodeName() => Inspector != null ? ($"Get {Inspector.VariableName}", s_DefaultTextColor) : ("Get Temp Variable", s_DefaultTextColor);

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
