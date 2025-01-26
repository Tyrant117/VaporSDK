using System;
using UnityEngine;
using Vapor.Inspector;

namespace Vapor.VisualScripting
{
    public class EqualToNode : IReturnNode<bool>
    {
        public uint Id { get; }
        public IGraph Graph { get; set; }
        
        public readonly IReturnNode<double> A;
        public readonly IReturnNode<double> B;
        
        private readonly string _aPort;
        private readonly int _aPortIndex;
        private readonly string _bPort;
        private readonly int _bPortIndex;

        public EqualToNode(string guid, NodePortTuple a, NodePortTuple b)
        {
            Id = guid.GetStableHashU32();
            A = (IReturnNode<double>)a.Node;
            _aPort = a.PortName;
            _aPortIndex = a.Index;
            B = (IReturnNode<double>)b.Node;
            _bPort = b.PortName;
            _bPortIndex = b.Index;
        }
        
        public object GetBoxedValue(IGraphOwner owner, int portIndex)
        {
            return GetValue(owner, portIndex);
        }
        
        public bool GetValue(IGraphOwner owner, int portIndex)
        {
            return Math.Abs(A.GetValue(owner, _aPortIndex) - B.GetValue(owner, _bPortIndex)) < 0.001;
        }
        
        public void Traverse(Action<INode> callback)
        {
            A.Traverse(callback);
            B.Traverse(callback);
            callback(this);
        }
    }
    
    [Serializable, SearchableNode("Logic/Equal", "Equal", "logic")]
    public class EqualToNodeData : NodeModel
    {
        private const string k_A = "a";
        private const string k_B = "b";
        private const string k_Result = "result";
        
        protected override void BuildAdditionalSlots()
        {
            InSlots.TryAdd(k_A, new PortSlot(k_A, "A", PortDirection.In, typeof(double))
                .WithContent<double>(0)
                .WithIndex(0));
            InSlots.TryAdd(k_B, new PortSlot(k_B, "B", PortDirection.In, typeof(double))
                .WithContent<double>(0)
                .WithIndex(1));

            OutSlots.TryAdd(k_Result, new PortSlot(k_Result, "Result", PortDirection.Out, typeof(bool))
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

            var sa = InSlots[k_A];
            var sb = InSlots[k_B];

            NodePortTuple a = sa.Reference.Guid.EmptyOrNull() ? new(new DoubleNode(Guid, (double)sa.Content), string.Empty, 0) : new(graph.Get(sa.Reference).Build(graph), sa.Reference.PortName, 0);
            NodePortTuple b = sb.Reference.Guid.EmptyOrNull() ? new(new DoubleNode(Guid, (double)sb.Content), string.Empty, 0) : new(graph.Get(sb.Reference).Build(graph), sb.Reference.PortName, 0);
            NodeRef = new EqualToNode(Guid, a, b);
            return NodeRef;
        }
        
        public override (string, Color) GetNodeName() => ("==", s_DefaultTextColor);
    }
}