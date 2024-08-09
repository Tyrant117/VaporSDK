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

namespace Vapor.Graphs
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

        public List<PortSlot> InSlots = new();
        public List<PortSlot> OutSlots = new();
        public virtual bool HasInPort => false;
        public List<NodeReference> In = new();

        public virtual bool HasOutPort => false;
        public List<NodeReference> Out = new();

        public NodeModel()
        {
            BuildSlots();
        }

        public virtual void BuildSlots()
        {
            if (HasInPort)
            {
                InSlots.Add(new PortSlot("in", "", PortDirection.In, typeof(NodeModel)));
            }
            if (HasOutPort)
            {
                OutSlots.Add(new PortSlot("out", "", PortDirection.Out, typeof(NodeModel)).CanAllowMultiple());
            }
        }

        public PortSlot GetInSlotWithName(string uniqueName) => InSlots.First(x => x.UniqueName == uniqueName);
        public PortSlot GetOutSlotWithName(string uniqueName) => OutSlots.First(x => x.UniqueName == uniqueName);

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
            InEdges.Sort((l, r) => l.InPortIndex.CompareTo(r.InPortIndex));
            OutEdges.Sort((l, r) => l.OutPortIndex.CompareTo(r.OutPortIndex));

            var fields = GetType().GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy);
            FindNodeAndConnectedPort(fields, nodesToLink);
        }

        [Conditional("UNITY_EDITOR")]
        private void FindNodeAndConnectedPort(FieldInfo[] fields, List<NodeModel> nodesToLink)
        {
            //UnityEngine.Debug.Log($"FindNodeAndConnectedPort: {fields.Length} | {nodesToLink.Count}");
            //foreach (var fi in fields)
            //{
            //    UnityEngine.Debug.Log($"FindNodeAndConnectedPort: {fi}");
            //}

            // Search the node for all fields that are marked with a NodeParam
            var ins = fields.Where(x => Attribute.IsDefined(x, typeof(PortInAttribute)) && x.GetCustomAttribute<PortAttribute>().Direction == PortDirection.In);
            var outs = fields.Where(x => Attribute.IsDefined(x, typeof(PortOutAttribute)) && x.GetCustomAttribute<PortAttribute>().Direction == PortDirection.Out);

            //UnityEngine.Debug.Log($"FindNodeAndConnectedPort: {ins.Count()} | {ins.Count()}");
            foreach (var port in ins)
            {
                // Get the NodeParam Attribute and then find the first Edge that matches that attributes port index.
                port.SetValue(this, new NodeReference("", 0));
                var atr = port.GetCustomAttribute<PortInAttribute>();
                var edge = InEdges.FirstOrDefault(e => e.InPortIndex == atr.PortIndex);

                if (edge != null)
                {
                    // In the list of all the nodes in the graph find the node that matches the edges Guid.
                    var node = nodesToLink.FirstOrDefault(node => edge.GuidMatches(node.Guid));
                    if (node != null)
                    {
                        port.SetValue(this, new NodeReference(node.Guid, edge.OutPortIndex)); // Set the port nodes value to this selected node.
                    }
                    else
                    {
                        InEdges.Remove(edge);
                    }
                }
                else
                {
                    Assert.IsFalse(atr.Required, $"Node:{Name} of type:{GetType()} has a PortInAttribute with the name {atr.Name} that is a required node," +
                        $" but there is no edge connecting to it in the graph.");
                }
            }

            foreach (var port in outs)
            {
                // Get the NodeParam Attribute and then find the first Edge that matches that attributes port index.
                port.SetValue(this, new NodeReference("", 0));
                var atr = port.GetCustomAttribute<PortOutAttribute>();
                var edge = OutEdges.FirstOrDefault(e => e.OutPortIndex == atr.PortIndex);

                if (edge != null)
                {
                    // In the list of all the nodes in the graph find the node that matches the edges Guid.
                    var node = nodesToLink.FirstOrDefault(x => edge.GuidMatches(x.Guid));
                    if (node != null)
                    {
                        port.SetValue(this, new NodeReference(node.Guid, edge.InPortIndex)); // Set the port nodes value to this selected node.
                    }
                    else
                    {
                        OutEdges.Remove(edge);
                    }
                }
                else
                {
                    Assert.IsFalse(atr.Required, $"Node:{Name} of type:{GetType()} has a PortOutAttribute with the name {atr.Name} that is a required node," +
                        $" but there is no edge connecting to it in the graph.");
                }
            }
        }        
    }
}
