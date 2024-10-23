using System;
using UnityEngine;
using Vapor.Inspector;
using Vapor.Keys;

namespace Vapor.VisualScripting
{
    public class MathGraphNode : IReturnNode<double>
    {
        public uint Id { get; }
        public IGraph Graph { get; set; }
        public ushort GraphKey { get; }

        public MathGraphNode(string guid, ushort graphKey)
        {
            Id = guid.GetStableHashU32();
            GraphKey = graphKey;
        }

        public double GetValue(IGraphOwner owner, string portName = "")
        {
            var graph = (MathGraph)RuntimeDataStore<IGraph>.Get(GraphKey);
            graph.Evaluate(owner);
            return graph.Value;
        }

        public void Traverse(Action<INode> callback)
        {
            callback(this);
        }
    }

    [Serializable, SearchableNode("Graphs/Math Graph", "math")]
    public class MathGraphNodeModel : NodeModel
    {
        private const string k_Result = "result";

        [Serializable]
        public class InspectorDrawer
        {
            [ValueDropdown("Graphs", ValueDropdownAttribute.FilterType.Category), OnValueChanged("OnStatChanged", false)]
            public KeyDropdownValue Stat;

            public NodeModel Owner { get; set; }

            private void OnStatChanged(KeyDropdownValue old, KeyDropdownValue @new)
            {
                Owner.OnRenameNode();
            }
        }

        public InspectorDrawer Inspector;

        public override void BuildSlots()
        {
            base.BuildSlots();

            OutSlots.TryAdd(k_Result, new PortSlot(k_Result, "Result", PortDirection.Out, typeof(double))
                .SetAllowMultiple()
                .SetIsOptional());
        }

        public override INode Build(GraphModel graph)
        {
            if (NodeRef != null)
            {
                return NodeRef;
            }

            NodeRef = new MathGraphNode(Guid, Inspector.Stat);
            return NodeRef;
        }

        public override (string, Color) GetNodeName() => Inspector != null ? (Inspector.Stat.DisplayName, s_DefaultTextColor) : (Name, s_DefaultTextColor);

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
