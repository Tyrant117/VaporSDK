using System;

namespace Vapor.VisualScripting
{
    //public class MathEvaluateNode : INode, IEvaluatorNode<double, IExternalValueSource>
    //{
    //    public uint Id { get; }

    //    public readonly IReturnNode<double> Start;

    //    private readonly int _startPort;

    //    public MathEvaluateNode(string guid, NodePortTuple start)
    //    {
    //        Id = guid.GetStableHashU32();
    //        Start = (IReturnNode<double>)start.Node;
    //        _startPort = start.PortName;
    //    }

    //    double IEvaluatorNode<double, IExternalValueSource>.Evaluate(GraphModel graph, IExternalValueSource arg)
    //    {
    //        return Start.GetValue(null);
    //    }
    //}

    //[Serializable, NodeName("Evaluate()"), NodeIsToken]
    //public class MathEvaluateNodeModel : NodeModel
    //{
    //    [PortIn("", 0, true, typeof(double))]
    //    public NodeReference Start;

    //    public override INode Build(GraphModel graph)
    //    {
    //        if (NodeRef != null)
    //        {
    //            return NodeRef;
    //        }

    //        NodeRef = new MathEvaluateNode(Guid, new(graph.Get(Start).Build(graph), Start.PortName));
    //        return NodeRef;
    //    }
    //}
}
