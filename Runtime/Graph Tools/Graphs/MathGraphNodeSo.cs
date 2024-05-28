using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Vapor.GraphTools
{
	public class MathGraphNodeSo : NodeSo
	{
		[SerializeField]
		private MathGraphSo _graph;
        public MathGraphSo Graph { get => _graph; set => _graph = value; }

		public List<NodeSo> LinkedProperties = new();

        public override void LinkNodeData(List<NodeSo> nodesToLink, Action<NodeSo> callback)
        {
            base.LinkNodeData(nodesToLink, callback);

            LinkedProperties.Clear();
            int idx = 0;
            foreach (var edge in InEdges)
            {
                var node = nodesToLink.First(node => edge.GuidMatches(node.GetGuid()));
                LinkedProperties.Add(node);
                node.LinkingGuid = Graph.ExposedProperties[idx].GetGuid();
                callback?.Invoke(node);
                idx++;
            }
        }
    }
}
