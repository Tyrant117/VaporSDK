using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.Assertions;
using Vapor.Inspector;

namespace Vapor.VisualScripting
{
    public class Node : INode
    {
        public uint Id { get; }

        public Node(string guid)
        {
            Id = guid.GetStableHashU32();
        }
    }

    [Serializable]
    public class NodeModel
    {
        protected const string k_In = "in";
        protected const string k_Out = "out";

        public static string CreateGuid() => System.Guid.NewGuid().ToString();

        public bool IsValid => !Guid.EmptyOrNull();
        public Type ToNodeType() => Type.GetType(NodeType);

        public string Name;
        public string NodeType;
        public string Guid;
        public string LinkingGuid;

        public Rect Position;

        public List<EdgeConnection> InEdges = new();
        public List<EdgeConnection> OutEdges = new();

        public Dictionary<string, PortSlot> InSlots = new();
        public Dictionary<string, PortSlot> OutSlots = new();
        public virtual bool HasInPort => false;
        public virtual bool HasOutPort => false;

        public NodeModel()
        {
            BuildSlots();
        }

        public virtual void BuildSlots()
        {
            if (HasInPort)
            {
                InSlots.TryAdd(k_In, new PortSlot(k_In, "", PortDirection.In, typeof(NodeModel)));
            }
            if (HasOutPort)
            {
                OutSlots.TryAdd(k_Out, new PortSlot(k_Out, "", PortDirection.Out, typeof(NodeModel)).SetAllowMultiple());
            }
        }

        [JsonIgnore, NonSerialized]
        protected INode NodeRef;
        public void Refresh() { NodeRef = null; }
        public virtual INode Build(GraphModel graph)
        {
            if (NodeRef != null)
            {
                return NodeRef;
            }

            NodeRef = new Node(Guid);
            return NodeRef;
        }

        [Conditional("UNITY_EDITOR")]
        public virtual void LinkNodeData(List<NodeModel> nodesToLink, Action<NodeModel> callback)
        {
            //InEdges.Sort((l, r) => l.InPortName.CompareTo(r.InPortName));
            //OutEdges.Sort((l, r) => l.OutPortName.CompareTo(r.OutPortName));


            FindNodeAndConnectedPort(nodesToLink);

            //var fields = GetType().GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy);
            //FindNodeAndConnectedPort(fields, nodesToLink);
        }

        [Conditional("UNITY_EDITOR")]
        private void FindNodeAndConnectedPort(List<NodeModel> nodesToLink)
        {
            foreach (var slot in InSlots.Values)
            {
                slot.Reference = new NodeReference(string.Empty, string.Empty);
                var edge = InEdges.FirstOrDefault(e => e.InSlot.SlotName == slot.UniqueName);
                if (edge.IsValid)
                {
                    var node = nodesToLink.FirstOrDefault(n => edge.OutputGuidMatches(n.Guid));
                    if (node != null)
                    {
                        slot.Reference = new NodeReference(node.Guid, edge.OutSlot.SlotName); // Set the port nodes value to this selected node.
                    }
                    else
                    {
                        InEdges.Remove(edge);
                    }
                }
                else
                {
                    Assert.IsTrue(slot.IsOptional, $"Node:[{Name}] of type:[{GetType()}] has a In Port with the name [{slot.UniqueName}] that is a required node," +
                        $" but there is no edge connecting to it in the graph.");
                }
            }

            foreach (var slot in OutSlots.Values)
            {
                slot.Reference = new NodeReference(string.Empty, string.Empty);
                var edge = OutEdges.FirstOrDefault(e => e.OutSlot.SlotName == slot.UniqueName);
                if (edge.IsValid)
                {
                    var node = nodesToLink.FirstOrDefault(n => edge.InputGuidMatches(n.Guid));
                    if (node != null)
                    {
                        slot.Reference = new NodeReference(node.Guid, edge.InSlot.SlotName); // Set the port nodes value to this selected node.
                    }
                    else
                    {
                        OutEdges.Remove(edge);
                    }
                }
                else
                {
                    Assert.IsTrue(slot.IsOptional, $"Node:[{Name}] of type:[{GetType()}] has a Out Port with the name [{slot.UniqueName}] that is a required node," +
                        $" but there is no edge connecting to it in the graph.");
                }
            }
        }    
    }
}
