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
        private List<EdgeConnection> _edges = new();
        public List<EdgeConnection> Edges { get => _edges; set => _edges = value; }

        
        public string GetGuid() => _guid;
        public void SetGuid(string guid) => _guid = guid;

        [Conditional("UNITY_EDITOR")]
        public virtual void LinkNodeData(List<NodeSo> nodesToLink, Action<NodeSo> callback)
        {
            Edges.Sort((l, r) => l.PortIndex.CompareTo(r.PortIndex));

            var fields = GetType().GetFields(BindingFlags.Public | BindingFlags.Instance);
            FindNodeAndConnectedPort(fields, nodesToLink);
        }

        [Conditional("UNITY_EDITOR")]
        private void FindNodeAndConnectedPort(FieldInfo[] fields, List<NodeSo> nodesToLink)
        {
            // Search the node for all fields that are marked with a NodeParam
            var ins = fields.Where(x => Attribute.IsDefined(x, typeof(NodeParamAttribute)));
            foreach (var port in ins)
            {
                // Get the NodeParam Attribute and then find the first Edge that matches that attributes port index.
                var atr = port.GetCustomAttribute<NodeParamAttribute>();
                var edge = Edges.FirstOrDefault(e => e.PortIndex == atr.PortIndex);

                if (edge != null)
                {
                    // In the list of all the nodes in the graph find the node that matches the edges Guid.
                    var node = nodesToLink.First(x => edge == x.GetGuid());
                    port.SetValue(this, node); // Set the port nodes value to this selected node.
                    var portIndex = GetType().GetField($"ConnectedPort_{port.Name}");
                    Assert.IsNotNull(portIndex, $"There is no public field with the name ConnectedPort_{port.Name}, all fields marked with a NodeParamAttribute," +
                        $" must have an integer field with the name public int ConnectedPort_<NodeParamAttribute.FieldName>");
                    portIndex.SetValue(this, edge.ConnectedPortIndex); // Set the port nodes connected port to the index that it matches in its child.
                }
                else
                {
                    Assert.IsFalse(atr.Required, $"Node:{name} of type:{GetType()} has a NodeParamAttribute with the name {atr.ParamName} that is a required node," +
                        $" but there is no edge connecting to it in the graph.");
                }
            }
        }
    }
}
