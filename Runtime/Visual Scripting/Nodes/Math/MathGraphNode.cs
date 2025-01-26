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

        private MathGraph _graph;

        public MathGraphNode(string guid, ushort graphKey)
        {
            Id = guid.GetStableHashU32();
            GraphKey = graphKey;
        }

        public object GetBoxedValue(IGraphOwner owner, int portIndex)
        {
            return GetValue(owner, portIndex);
        }

        public double GetValue(IGraphOwner owner, int portIndex)
        {
            _graph ??= (MathGraph)RuntimeDataStore<IGraph>.Get(GraphKey);
            _graph.Evaluate(owner);
            return _graph.Value;
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

        protected override void BuildAdditionalSlots()
        {
            OutSlots.TryAdd(k_Result, new PortSlot(k_Result, "Result", PortDirection.Out, typeof(double))
                .SetAllowMultiple()
                .SetIsOptional()
                .WithIndex(0));
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
