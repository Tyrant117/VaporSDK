using UnityEngine;

namespace Vapor.VisualScripting
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
