using System;
using Vapor.Inspector;

namespace Vapor.VisualScripting
{
    public class PowerNode : INode, IReturnNode<double>
    {
        public uint Id { get; }
        public IGraph Graph { get; set; }

        public readonly IReturnNode<double> A;
        public readonly IReturnNode<double> B;

        private readonly string _aPort;
        private readonly string _bPort;

        public PowerNode(string guid, NodePortTuple a, NodePortTuple b)
        {
            Id = guid.GetStableHashU32();
            A = (IReturnNode<double>)a.Node;
            _aPort = a.PortName;
            B = (IReturnNode<double>)b.Node;
            _bPort = b.PortName;

        }

        public double GetValue(IGraphOwner owner, string portName = "")
        {
            return Math.Pow(A.GetValue(owner, _aPort), B.GetValue(owner, _bPort));
        }

        public void Traverse(Action<INode> callback)
        {
            A.Traverse(callback);
            B.Traverse(callback);
            callback(this);
        }
    }

    [Serializable, SearchableNode("Math/Basic/Power", "Power", "math")]
    public class PowerNodeModel : NodeModel
    {
        private const string k_A = "a";
        private const string k_B = "b";
        private const string k_Result = "result";

        public override void BuildSlots()
        {
            base.BuildSlots();
            InSlots.TryAdd(k_A, new PortSlot(k_A, "A", PortDirection.In, typeof(double))
                .WithContent<double>(0));
            InSlots.TryAdd(k_B, new PortSlot(k_B, "Exp", PortDirection.In, typeof(double))
                .WithContent<double>(0));

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

            var sa = InSlots[k_A];
            var sb = InSlots[k_B];

            NodePortTuple a = sa.Reference.Guid.EmptyOrNull() ? new(new DoubleNode(Guid, (double)sa.Content), string.Empty) : new(graph.Get(sa.Reference).Build(graph), sa.Reference.PortName);
            NodePortTuple b = sb.Reference.Guid.EmptyOrNull() ? new(new DoubleNode(Guid, (double)sb.Content), string.Empty) : new(graph.Get(sb.Reference).Build(graph), sb.Reference.PortName);
            NodeRef = new PowerNode(Guid, a, b);
            return NodeRef;
        }
    }
}
