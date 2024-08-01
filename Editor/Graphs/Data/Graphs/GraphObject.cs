using System;
using UnityEngine;
using Vapor.Graphs;

namespace VaporEditor.Graphs
{
    public class GraphObject : ScriptableObject
    {
        public Type GraphType;
        public Graph Graph;

        public void Setup(Graph graph)
        {
            Graph = graph;
            GraphType = Graph.GetType();
        }

        public void Validate()
        {

        }
    }
}
