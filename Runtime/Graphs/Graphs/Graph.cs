using System.Collections.Generic;
using UnityEngine;

namespace Vapor.Graphs
{
    [System.Serializable]
    public class Graph
    {
        public string AssemblyQualifiedType;
        public Node Root;
        public List<Node> Children;
    }
}
