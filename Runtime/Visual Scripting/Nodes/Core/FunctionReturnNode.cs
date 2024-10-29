using System;
using System.Collections.Generic;
using UnityEngine;
using Vapor.VisualScripting;

namespace Vapor.VisualScripting
{
    public class FunctionReturnNode : IImpureNode
    {
        public uint Id { get; }
        public IGraph Graph
        {
            get => _graph;
            set => _graph = (FunctionGraph)value;
        }
        public IImpureNode Next { get; set; }

        private FunctionGraph _graph;
        private readonly List<IReturnNode> _returnNodes;

        public FunctionReturnNode(string guid, List<NodePortTuple> portTuples)
        {
            Id = guid.GetStableHashU32();

            _returnNodes = new();
            foreach (var t in portTuples)
            {
                _returnNodes.Add((IReturnNode)t.Node);
            }
        }

        public void Traverse(Action<INode> callback)
        {
            foreach (var n in _returnNodes)
            {
                n.Traverse(callback);
            }
            callback(this);
        }

        public void Invoke(IGraphOwner owner)
        {
            for (int i = 0; i < _returnNodes.Count; i++)
            {
                var val = _returnNodes[i].GetBoxedValue(owner, i);
                _graph.SetReturnData(i.ToString(), val);
            }
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
            if (returnTypes != null)
            {
                ReturnTypes.AddRange(returnTypes);
            }

            BuildSlots();
        }

        public override void BuildSlots()
        {
            ReturnTypes ??= new();
            InSlots.Clear();

            base.BuildSlots();
            int idx = 1;
            foreach (var rt in ReturnTypes)
            {
                InSlots.TryAdd(rt.Item1, new PortSlot(rt.Item1, rt.Item1, PortDirection.In, rt.Item2)
                    .WithContent(rt.Item2)
                    .WithIndex(idx));
                idx++;
            }
        }

        public override INode Build(GraphModel graph)
        {
            if (NodeRef != null)
            {
                return NodeRef;
            }

            List<NodePortTuple> portTuples = new();
            foreach (var resultPort in InSlots)
            {
                if (resultPort.Key == k_In)
                {
                    continue;
                }

                portTuples.Add(new(graph.Get(resultPort.Value.Reference).Build(graph), resultPort.Value.Reference.PortName, resultPort.Value.Index));
            }
            portTuples.Sort((l, r) => l.Index.CompareTo(r.Index));

            NodeRef = new FunctionReturnNode(Guid, portTuples);
            return NodeRef;
        }

        public override (string, Color) GetNodeName() => ("Return", s_DefaultTextColor);
    }
}
