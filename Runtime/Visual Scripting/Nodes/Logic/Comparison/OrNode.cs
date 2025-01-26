using System;
using UnityEngine;
using Vapor.Inspector;

namespace Vapor.VisualScripting
{
    public class OrNode : IReturnNode<bool>
    {
        public uint Id { get; }
        public IGraph Graph { get; set; }
        
        public readonly IReturnNode<bool> A;
        public readonly IReturnNode<bool> B;
        
        private readonly string _aPort;
        private readonly int _aPortIndex;
        private readonly string _bPort;
        private readonly int _bPortIndex;

        public OrNode(string guid, NodePortTuple a, NodePortTuple b)
        {
            Id = guid.GetStableHashU32();
            A = (IReturnNode<bool>)a.Node;
            _aPort = a.PortName;
            _aPortIndex = a.Index;
            B = (IReturnNode<bool>)b.Node;
            _bPort = b.PortName;
            _bPortIndex = b.Index;
        }
        
        public object GetBoxedValue(IGraphOwner owner, int portIndex)
        {
            return GetValue(owner, portIndex);
        }
        
        public bool GetValue(IGraphOwner owner, int portIndex)
        {
            return A.GetValue(owner, _aPortIndex) || B.GetValue(owner, _bPortIndex);
        }
        
        public void Traverse(Action<INode> callback)
        {
            A.Traverse(callback);
            B.Traverse(callback);
            callback(this);
        }
    }
    
    [Serializable, SearchableNode("Logic/Or", "Or", "logic")]
    public class OrNodeData : NodeModel
    {
        private const string k_A = "a";
        private const string k_B = "b";
        private const string k_Result = "result";
        
        protected override void BuildAdditionalSlots()
        {
            InSlots.TryAdd(k_A, new PortSlot(k_A, "A", PortDirection.In, typeof(bool))
                .WithContent<bool>(false)
                .WithIndex(0));
            InSlots.TryAdd(k_B, new PortSlot(k_B, "B", PortDirection.In, typeof(bool))
                .WithContent<bool>(false)
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

            NodePortTuple a = sa.Reference.Guid.EmptyOrNull() ? new(new BoolNode(Guid, (bool)sa.Content), string.Empty, 0) : new(graph.Get(sa.Reference).Build(graph), sa.Reference.PortName, 0);
            NodePortTuple b = sb.Reference.Guid.EmptyOrNull() ? new(new BoolNode(Guid, (bool)sb.Content), string.Empty, 0) : new(graph.Get(sb.Reference).Build(graph), sb.Reference.PortName, 0);
            NodeRef = new OrNode(Guid, a, b);
            return NodeRef;
        }
        
        public override (string, Color) GetNodeName() => ("||", s_DefaultTextColor);
    }
}