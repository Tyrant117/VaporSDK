using UnityEngine;

namespace Vapor.Graphs
{
    [System.Serializable]
    public class MathGraph : Graph, IEvaluatorNode<double, IExternalValueSource>
    {
        private bool _isInit;
        private MathRootNode _root;

        public MathGraph()
        {
            AssemblyQualifiedType = GetType().AssemblyQualifiedName;
        }

        public double Evaluate(IExternalValueSource arg)
        {
            if(!_isInit)
            {
                _root = (MathRootNode)Root;
                _isInit = true;
            }

            return _root.Evaluate(arg);
        }
    }
}
