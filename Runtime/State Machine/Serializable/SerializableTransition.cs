using UnityEngine;
using Vapor.Inspector;

namespace Vapor.StateMachines
{
    [System.Serializable]
    public class SerializableTransition
    {
        public enum Type
        {
            WaitForTrue,
            TimedTransition,
            WaitForCoroutine
        }
        
        public string FromStateName;
        public string ToStateName;
        public int Desire;
        public Type TransitionType;
        [RichTextTooltip("The name of the method that will be used to evaluate this transition.")]
        public string MethodEvaluatorName;
    }
}
