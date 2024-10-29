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
        public IGraph Graph { get; set; }

        public Node(string guid)
        {
            Id = guid.GetStableHashU32();
        }

        public void Traverse(Action<INode> callback)
        {
            callback(this);
        }
    }

    [Serializable]
    public class NodeModel
    {
        protected const string k_In = "in";
        protected const string k_Out = "out";
        protected static Color s_DefaultTextColor = new(0.7568628f, 0.7568628f, 0.7568628f);

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

        [JsonIgnore]
        public Action RenameNode;
        public void OnRenameNode()
        {
            RenameNode?.Invoke();
        }

        public NodeModel()
        {
            BuildSlots();
        }

        public virtual void BuildSlots()
        {
            if (HasInPort)
            {
                InSlots.TryAdd(k_In, new PortSlot(k_In, "", PortDirection.In, typeof(NodeModel)).WithIndex(0));
            }
            if (HasOutPort)
            {
                OutSlots.TryAdd(k_Out, new PortSlot(k_Out, "", PortDirection.Out, typeof(NodeModel)).SetAllowMultiple().WithIndex(0));
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

        public virtual (string, Color) GetNodeName() => (Name, s_DefaultTextColor);
        public virtual (Sprite, Color) GetNodeNameIcon() => (null, Color.white);
        public virtual object GraphSettingsInspector() => null;

        [Conditional("UNITY_EDITOR")]
        public virtual void LinkNodeData(List<NodeModel> nodesToLink, Action<NodeModel> callback)
        {
            FindNodeAndConnectedPort(nodesToLink);
        }

        [Conditional("UNITY_EDITOR")]
        private void FindNodeAndConnectedPort(List<NodeModel> nodesToLink)
        {
            List<EdgeConnection> GoodEdges = new();
            foreach (var slot in InSlots.Values)
            {
                slot.Reference = new NodeReference(string.Empty, string.Empty, 0);
                var edge = InEdges.FirstOrDefault(e => e.InSlot.SlotName == slot.UniqueName);
                if (edge.IsValid)
                {
                    var node = nodesToLink.FirstOrDefault(n => edge.OutputGuidMatches(n.Guid));
                    if (node != null)
                    {
                        slot.Reference = new NodeReference(node.Guid, edge.OutSlot.SlotName, edge.OutSlot.Index); // Set the port nodes value to this selected node.
                        GoodEdges.Add(edge);
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

            InEdges.Clear();
            InEdges.AddRange(GoodEdges);

            GoodEdges.Clear();
            foreach (var slot in OutSlots.Values)
            {
                slot.Reference = new NodeReference(string.Empty, string.Empty, 0);
                var edge = OutEdges.FirstOrDefault(e => e.OutSlot.SlotName == slot.UniqueName);
                if (edge.IsValid)
                {
                    var node = nodesToLink.FirstOrDefault(n => edge.InputGuidMatches(n.Guid));
                    if (node != null)
                    {
                        slot.Reference = new NodeReference(node.Guid, edge.InSlot.SlotName, edge.InSlot.Index); // Set the port nodes value to this selected node.
                        GoodEdges.Add(edge);
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

                OutEdges.Clear();
                OutEdges.AddRange(GoodEdges);
            }
        }    
    }
}
