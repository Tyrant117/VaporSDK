using System;
using System.Collections.Generic;
using UnityEngine;
using Vapor.VisualScripting;

namespace Vapor.VisualScripting
{
    public class FunctionReturnNode : IImpureNode
    {
        public uint Id { get; }
        public IGraph Graph { get; set; }
        //public IImpureNode Previous { get; set; }
        public IImpureNode Next { get; set; }

        private readonly string _previousPort;

        public FunctionReturnNode(string guid)
        {
            Id = guid.GetStableHashU32();
            //Previous = (IImpureNode)previous.Node;
            //_previousPort = previous.PortName;
        }

        public void Traverse(Action<INode> callback)
        {
            callback(this);
        }

        public void Invoke(IGraphOwner owner)
        {

        }
    }

    public class FunctionReturnNodeModel : NodeModel
    {
        public override bool HasInPort => true;
        
        protected List<(string, Type)> ReturnTypes;


        public void UpdateReturnValues(List<(string, Type)> returnTypes)
        {
            ReturnTypes ??= new();
            ReturnTypes.Clear();
            ReturnTypes.AddRange(returnTypes);

            BuildSlots();
            Debug.Log("Built New Slots");
        }

        public override void BuildSlots()
        {
            ReturnTypes ??= new();
            InSlots.Clear();

            base.BuildSlots();
            foreach (var rt in ReturnTypes)
            {
                InSlots.TryAdd(rt.Item1, new PortSlot(rt.Item1, rt.Item1, PortDirection.In, rt.Item2)
                    .WithContent(rt.Item2));
            }
        }

        public override INode Build(GraphModel graph)
        {
            if (NodeRef != null)
            {
                return NodeRef;
            }

            //var sIn = InSlots[k_In];
            //NodePortTuple @in = new(graph.Get(sIn.Reference).Build(graph), sIn.Reference.PortName);

            NodeRef = new FunctionReturnNode(Guid);
            return NodeRef;
        }

        public override (string, Color) GetNodeName() => ("Return", s_DefaultTextColor);
    }
}
