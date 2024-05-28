using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Assertions;

namespace Vapor.GraphTools
{
    public class NodeSo : ScriptableObject, IGuidNode, IGraphPosition
    {
        [SerializeField]
        private string _guid;
        [SerializeField]
        private string _linkingGuid;
        public string LinkingGuid { get => _linkingGuid; set => _linkingGuid = value; }

        [SerializeField]
        private Rect _position;
        public Rect Position { get => _position; set => _position = value; }

        [SerializeField]
        private string _name;        
        public string Name { get => _name; set => _name = value; }

        [SerializeField]
        private List<EdgeConnection> _inEdges = new();
        public List<EdgeConnection> InEdges { get => _inEdges; set => _inEdges = value; }

        [SerializeField]
        private List<EdgeConnection> _outEdges = new();
        public List<EdgeConnection> OutEdges { get => _outEdges; set => _outEdges = value; }


        public string GetGuid() => _guid;
        public void SetGuid(string guid) => _guid = guid;

        [Conditional("UNITY_EDITOR")]
        public virtual void LinkNodeData(List<NodeSo> nodesToLink, Action<NodeSo> callback)
        {
            InEdges.Sort((l, r) => l.InPortIndex.CompareTo(r.InPortIndex));
            OutEdges.Sort((l, r) => l.OutPortIndex.CompareTo(r.OutPortIndex));

            var fields = GetType().GetFields(BindingFlags.Public | BindingFlags.Instance);
            FindNodeAndConnectedPort(fields, nodesToLink);
        }

        [Conditional("UNITY_EDITOR")]
        private void FindNodeAndConnectedPort(FieldInfo[] fields, List<NodeSo> nodesToLink)
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
                    var node = nodesToLink.First(node => edge.GuidMatches(node.GetGuid()));
                    port.SetValue(this, node); // Set the port nodes value to this selected node.
                    var portIndex = GetType().GetField($"InConnectedPort_{port.Name}");
                    Assert.IsNotNull(portIndex, $"There is no public field with the name InConnectedPort_{port.Name}, all fields marked with a PortInAttribute," +
                        $" must have an integer field with the name public int InConnectedPort_<PortInAttribute.FieldName>");
                    portIndex.SetValue(this, edge.OutPortIndex); // Set the port nodes connected port to the index that it matches in its child.
                }
                else
                {
                    Assert.IsFalse(atr.Required, $"Node:{name} of type:{GetType()} has a PortInAttribute with the name {atr.Name} that is a required node," +
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
                    var node = nodesToLink.First(x => edge.GuidMatches(x.GetGuid()));
                    port.SetValue(this, node); // Set the port nodes value to this selected node.
                    var portIndex = GetType().GetField($"OutConnectedPort_{port.Name}");
                    Assert.IsNotNull(portIndex, $"There is no public field with the name OutConnectedPort_{port.Name}, all fields marked with a PortOutAttribute," +
                        $" must have an integer field with the name public int OutConnectedPort_<PortOutAttribute.FieldName>");
                    portIndex.SetValue(this, edge.InPortIndex); // Set the port nodes connected port to the index that it matches in its child.
                }
                else
                {
                    Assert.IsFalse(atr.Required, $"Node:{name} of type:{GetType()} has a PortOutAttribute with the name {atr.Name} that is a required node," +
                        $" but there is no edge connecting to it in the graph.");
                }
            }
        }
    }
}
