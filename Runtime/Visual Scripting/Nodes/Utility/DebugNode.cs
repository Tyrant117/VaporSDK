using System;
using UnityEngine;

namespace Vapor.VisualScripting
{
    public class DebugNode : IImpureNode
    {
        public uint Id { get; }
        public IGraph Graph { get; set; }
        public IImpureNode Next { get; set; }
        public string DebugText { get; }

        public DebugNode(string guid, string debugText)
        {
            Id = guid.GetStableHashU32();
            DebugText = debugText;
        }

        public void Invoke(IGraphOwner owner)
        {
            Debug.Log(DebugText);
            Next.Invoke(owner);
        }

        public void Traverse(Action<INode> callback)
        {
            callback(this);
            Next.Traverse(callback);
        }
    }

    [Serializable, SearchableNode("Util/DebugLog", "util")]
    public class DebugNodeModel : NodeModel
    {
        public override bool HasInPort => true;
        public override bool HasOutPort => true;

        [Serializable]
        public class InspectorDrawer
        {
            [TextArea(3, 5)]
            public string DebugText;

            public NodeModel Owner { get; set; }
        }

        public InspectorDrawer Inspector;

        public override void BuildSlots()
        {
            base.BuildSlots();
        }

        public override INode Build(GraphModel graph)
        {
            if (NodeRef != null)
            {
                return NodeRef;
            }

            var refN = new DebugNode(Guid, Inspector.DebugText);
            NodeRef = refN;

            var sNext = OutSlots[k_Out];
            NodePortTuple next = new(graph.Get(sNext.Reference).Build(graph), sNext.Reference.PortName, 0);
            refN.Next = (IImpureNode)next.Node;
            return NodeRef;
        }

        public override (string, Color) GetNodeName() => ("Debug Log", s_DefaultTextColor);

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
