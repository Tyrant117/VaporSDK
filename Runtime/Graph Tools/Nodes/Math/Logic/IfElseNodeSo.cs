using System;

namespace Vapor.GraphTools
{
    [SearchableNode("Math/Logic/If Else", "If Else", "math")]
    public class IfElseNodeSo : MathNodeSo
    {
        [PortIn("If", 0, true, typeof(bool))]
        public NodeSo If;
        [PortIn("True", 1, true, typeof(float))]
        public NodeSo True;
        [PortIn("False", 2, true, typeof(float))]
        public NodeSo False;

        [PortOut("Out", 0, true, typeof(float))]
        public NodeSo Out;

        public int InConnectedPort_If;
        public int InConnectedPort_True;
        public int InConnectedPort_False;
        public int OutConnectedPort_Out;

        [NonSerialized]
        private bool _hasInit;
        [NonSerialized]
        private IEvaluatorNode<bool> _if;
        [NonSerialized]
        private IEvaluatorNode<float> _true;
        [NonSerialized]
        private IEvaluatorNode<float> _false;

        public override float Evaluate(IExternalValueGetter externalValues, int portIndex)
        {
            if (!_hasInit)
            {
                _if = (IEvaluatorNode<bool>)If;
                _true = (IEvaluatorNode<float>)True;
                _false = (IEvaluatorNode<float>)False;
                _hasInit = true;
            }

            return _if.Evaluate(externalValues, InConnectedPort_If) ? _true.Evaluate(externalValues, InConnectedPort_True) : _false.Evaluate(externalValues, InConnectedPort_False);
        }
    }
}
