using System;

namespace Vapor.VisualScripting
{
    public class BoolNode : IReturnNode<bool>
    {
        public uint Id { get; }
        public IGraph Graph { get; set; }
        public bool Value { get; }

        public BoolNode(string guid, bool value)
        {
            Id = guid.GetStableHashU32();
            Value = value;
        }

        public object GetBoxedValue(IGraphOwner owner, int portIndex) => Value;
        public bool GetValue(IGraphOwner owner, int portIndex) => Value;

        public void Traverse(Action<INode> callback)
        {
            callback(this);
        }
    }
    
    [Serializable, SearchableNode("Value Types/Bool", "Bool"), NodeIsToken]
    public class BoolNodeModel : ValueNode<bool>
    {
        [PortOut("Value", 0, true, true, typeof(bool))]
        public NodeReference Out;

        public override INode Build(GraphModel graph)
        {
            if (NodeRef != null)
            {
                return NodeRef;
            }

            NodeRef = new BoolNode(Guid, Value);
            return NodeRef;
        }
    }
}