using System;

namespace Vapor.VisualScripting
{
    public class DoubleNode : INode, IReturnNode<double>
    {
        public uint Id { get; }
        public double Value { get; }

        public DoubleNode(string guid, double value)
        {
            Id = guid.GetStableHashU32();
            Value = value;
        }

        public double GetValue(IGraphOwner owner, string portName = "") => Value;
    }

    [Serializable, SearchableNode("Value Types/Double", "Double"), NodeIsToken]
    public class DoubleNodeModel : ValueNode<double>
    {
        [PortOut("Value", 0, true, true, typeof(double))]
        public NodeReference Out;

        public override INode Build(GraphModel graph)
        {
            if (NodeRef != null)
            {
                return NodeRef;
            }

            NodeRef = new DoubleNode(Guid, Value);
            return NodeRef;
        }
    }
}
