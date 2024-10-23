using System;
using UnityEngine;
using Vapor.Inspector;

namespace Vapor.VisualScripting
{
    public class MathReturnNode : IReturnNode<double>
    {
        public uint Id { get; }
        public IGraph Graph { get; set; }

        public readonly IReturnNode<double> Result;

        private readonly string _resultPort;

        public MathReturnNode(string guid, NodePortTuple result)
        {
            Id = guid.GetStableHashU32();
            Result = (IReturnNode<double>)result.Node;
            _resultPort = result.PortName;
        }

        public double GetValue(IGraphOwner owner, string portName = "")
        {
            return Result.GetValue(owner, _resultPort);
        }

        public void Traverse(Action<INode> callback)
        {
            Result.Traverse(callback);
            callback(this);
        }
    }

    public class MathReturnNodeModel : NodeModel
    {
        private const string k_Result = "result";

        public override void BuildSlots()
        {
            base.BuildSlots();
            InSlots.TryAdd(k_Result, new PortSlot(k_Result, "Result", PortDirection.In, typeof(double))
                .WithContent<double>(0));
        }

        public override INode Build(GraphModel graph)
        {
            if (NodeRef != null)
            {
                return NodeRef;
            }

            var sResult = InSlots[k_Result];

            NodePortTuple result = sResult.Reference.Guid.EmptyOrNull() ? new(new DoubleNode(Guid, (double)sResult.Content), string.Empty) : new(graph.Get(sResult.Reference).Build(graph), sResult.Reference.PortName);
            NodeRef = new MathReturnNode(Guid, result);
            return NodeRef;
        }

        public override (string, Color) GetNodeName() => ("Result", s_DefaultTextColor);
    }
}
