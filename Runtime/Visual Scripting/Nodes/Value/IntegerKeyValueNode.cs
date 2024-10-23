using System;
using Vapor.Keys;

namespace Vapor.VisualScripting
{
    public class IntegerKVN : INode, IValueNode<int>
    {
        public uint Id { get; }
        public IGraph Graph { get; set; }
        public int Value { get; }


        public IntegerKVN(string guid, int value)
        {
            Id = guid.GetStableHashU32();
            Value = value;
        }

        public int GetValue(int portIndex)
        {
            return Value;
        }

        public void Traverse(Action<INode> callback)
        {
            callback(this);
        }
    }

    public class IntegerKeyValueNode : UnityObjectValueNode<IntegerKeySo>
    {      
        public virtual int FromID { get; } = -1;

        public override INode Build(GraphModel graph)
        {
            if(NodeRef != null)
            {
                return NodeRef;
            }

            NodeRef = new IntegerKVN(Guid, Value.Key);
            return NodeRef;
        }
    }
}
