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
    [Serializable, DrawWithVapor]
    public class Node
    {
        public static string CreateGuid() => System.Guid.NewGuid().ToString();

        public string Guid;
        public string LinkingGuid;

        public Rect Position;

        public string Name;

        public List<EdgeConnection> InEdges = new();
        public List<EdgeConnection> OutEdges = new();

        [Conditional("UNITY_EDITOR")]
        public virtual void ToCSharp(StringBuilder sb) { }

        [Conditional("UNITY_EDITOR")]
        public virtual void LinkNodeData(List<Node> nodesToLink, Action<Node> callback)
        {
            InEdges.Sort((l, r) => l.InPortIndex.CompareTo(r.InPortIndex));
            OutEdges.Sort((l, r) => l.OutPortIndex.CompareTo(r.OutPortIndex));

            var fields = GetType().GetFields(BindingFlags.Public | BindingFlags.Instance);
            FindNodeAndConnectedPort(fields, nodesToLink);
        }

        [Conditional("UNITY_EDITOR")]
        private void FindNodeAndConnectedPort(FieldInfo[] fields, List<Node> nodesToLink)
        {
            // Search the node for all fields that are marked with a NodeParam
            var ins = fields.Where(x => Attribute.IsDefined(x, typeof(PortInAttribute)));
            var outs = fields.Where(x => Attribute.IsDefined(x, typeof(PortOutAttribute)));
            foreach (var port in ins)
            {
                // Get the NodeParam Attribute and then find the first Edge that matches that attributes port index.
                var atr = port.GetCustomAttribute<PortInAttribute>();
                var edge = InEdges.FirstOrDefault(e => e.InPortIndex == atr.PortIndex);

                if (edge != null)
                {
                    // In the list of all the nodes in the graph find the node that matches the edges Guid.
                    var node = nodesToLink.First(node => edge.GuidMatches(node.Guid));
                    port.SetValue(this, node); // Set the port nodes value to this selected node.
                    var portIndex = GetType().GetField($"InConnectedPort_{port.Name}");
                    Assert.IsNotNull(portIndex, $"There is no public field with the name InConnectedPort_{port.Name} in {GetType()}, all fields marked with a PortInAttribute," +
                        $" must have an integer field with the name public int InConnectedPort_<PortInAttribute.FieldName>");
                    portIndex.SetValue(this, edge.OutPortIndex); // Set the port nodes connected port to the index that it matches in its child.
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
                var atr = port.GetCustomAttribute<PortOutAttribute>();
                var edge = OutEdges.FirstOrDefault(e => e.OutPortIndex == atr.PortIndex);

                if (edge != null)
                {
                    // In the list of all the nodes in the graph find the node that matches the edges Guid.
                    var node = nodesToLink.First(x => edge.GuidMatches(x.Guid));
                    port.SetValue(this, node); // Set the port nodes value to this selected node.
                    var portIndex = GetType().GetField($"OutConnectedPort_{port.Name}");
                    Assert.IsNotNull(portIndex, $"There is no public field with the name OutConnectedPort_{port.Name} in {GetType()}, all fields marked with a PortOutAttribute," +
                        $" must have an integer field with the name public int OutConnectedPort_<PortOutAttribute.FieldName>");
                    portIndex.SetValue(this, edge.InPortIndex); // Set the port nodes connected port to the index that it matches in its child.
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
