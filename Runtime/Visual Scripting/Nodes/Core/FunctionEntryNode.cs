using System;
using System.Collections.Generic;
using UnityEngine;

namespace Vapor.VisualScripting
{
    public class FunctionEntryNode : IImpureNode, IHasValuePorts
    {
        public uint Id { get; }
        public IGraph Graph { get; set; }
        public IImpureNode Next { get; set; }

        private readonly string _nextPort;

        public FunctionEntryNode(string guid)
        {
            Id = guid.GetStableHashU32();
        }

        public void Traverse(Action<INode> callback)
        {
            callback(this);
            Next.Traverse(callback);
        }

        public T GetValue<T>(IGraphOwner owner, string portName)
        {
            if (Graph is FunctionGraph functionGraph)
            {
                return functionGraph.GetInputValue<T>(portName);
            }

            return default(T);
        }

        public void Invoke(IGraphOwner owner)
        {
            Next.Invoke(owner);
        }
    }

    public class FunctionEntryNodeModel : NodeModel
    {
        public override bool HasOutPort => true;

        protected List<(string, Type)> InputTypes;

        public void UpdateInputValues(List<(string, Type)> inputTypes)
        {
            InputTypes ??= new();
            InputTypes.Clear();
            if (inputTypes != null)
            {
                InputTypes.AddRange(inputTypes);
            }

            OutSlots.Clear();
            BuildSlots(true);
        }

        protected override void BuildAdditionalSlots()
        {
            InputTypes ??= new List<(string, Type)>();
            
            foreach (var rt in InputTypes)
            {
                OutSlots.TryAdd(rt.Item1, new PortSlot(rt.Item1, rt.Item1, PortDirection.Out, rt.Item2)
                    .WithContent(rt.Item2));
            }
        }

        public override INode Build(GraphModel graph)
        {
            if (NodeRef != null)
            {
                return NodeRef;
            }


            var refN = new FunctionEntryNode(Guid);
            NodeRef = refN;

            var sNext = OutSlots[k_Out];
            NodePortTuple next = new(graph.Get(sNext.Reference).Build(graph), sNext.Reference.PortName, 0);
            refN.Next = (IImpureNode)next.Node;
            return NodeRef;
        }

        public override (string, Color) GetNodeName() => ("Entry", s_DefaultTextColor);
    }
}
