using System.Collections.Generic;
using UnityEngine;
using Vapor.Inspector;

namespace Vapor.Graphs
{
    public class GraphSo : VaporScriptableObject
    {
        [ReadOnly]
        public string GraphType;

        public List<string> SearchIncludeFlags = new();

        [ReadOnly, TextArea(50,100)]
        public string JsonGraph;
    }
}
