using System;
using UnityEngine;
using Vapor.Inspector;

namespace Vapor.VisualScripting
{
    public class NotNode : IReturnNode<bool>
    {
        public uint Id { get; }
        public IGraph Graph { get; set; }
        
        public readonly IReturnNode<bool> A;
        
        private readonly string _aPort;
        private readonly int _aPortIndex;

        public NotNode(string guid, NodePortTuple a)
        {
            Id = guid.GetStableHashU32();
            A = (IReturnNode<bool>)a.Node;
            _aPort = a.PortName;
            _aPortIndex = a.Index;
        }
        
        public object GetBoxedValue(IGraphOwner owner, int portIndex)
        {
            return GetValue(owner, portIndex);
        }
        
        public bool GetValue(IGraphOwner owner, int portIndex)
        {
            return !A.GetValue(owner, _aPortIndex);
        }
        
        public void Traverse(Action<INode> callback)
        {
            A.Traverse(callback);
            callback(this);
        }
    }
    
    [Serializable, SearchableNode("Logic/Not", "Not", "logic")]
    public class NotNodeData : NodeModel
    {
        private const string k_A = "a";
        private const string k_B = "b";
        private const string k_Result = "result";
        
        protected override void BuildAdditionalSlots()
        {
            InSlots.TryAdd(k_A, new PortSlot(k_A, "A", PortDirection.In, typeof(bool))
                .WithContent<bool>(false)
                .WithIndex(0));

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
            NodeRef = new NotNode(Guid, a);
            return NodeRef;
        }
        
        public override (string, Color) GetNodeName() => ("!", s_DefaultTextColor);
    }
}