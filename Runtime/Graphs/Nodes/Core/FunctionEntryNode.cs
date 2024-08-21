using UnityEngine;

namespace Vapor.Graphs
{
    public class FunctionEntryNode : INode
    {
        public uint Id { get; }
    }

    [NodeName("Entry")]
    public class FunctionEntryNodeModel : NodeModel
    {
        public override bool HasOutPort => true;
    }
}
