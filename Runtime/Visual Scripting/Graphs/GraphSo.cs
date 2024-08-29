using System.Collections.Generic;
using UnityEngine;
using Vapor.Inspector;

namespace Vapor.VisualScripting
{
    public class GraphSo : VaporScriptableObject
    {
        public List<string> SearchIncludeFlags = new();

        [Title("Graph")]
        [ReadOnly]
        public string ModelType;
        [ReadOnly, TextArea(50, 100)]
        public string ModelJson;
    }
}
