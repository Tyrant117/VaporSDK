using System.Collections.Generic;
using UnityEngine;
using Vapor.Inspector;

namespace Vapor.GraphTools
{
    public class MathGraphSo : VaporScriptableObject
    {
        [SerializeField]
        private MathEvaluateNodeSo _root;
        public MathEvaluateNodeSo Root { get => _root; set => _root = value; }

        [SerializeField]
        private List<ExposedPropertyNodeSo> _exposedProperties = new();
        public List<ExposedPropertyNodeSo> ExposedProperties => _exposedProperties;

        public float EvaluateGraph(IExternalValueGetter getter)
        {
            return Root.Evaluate(getter, -1);
        }
    }
}
