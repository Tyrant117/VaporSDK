using System.Collections.Generic;
using UnityEngine;

namespace Vapor.StateMachines
{
    [System.Serializable]
    public class SerializableState
    {
        public enum Type
        {
            State,
            CoroutineState,
            AsyncState,
        }
        
        public string Name;
        public bool CanExitInstantly;
        public bool CanTransitionToSelf;
        public Type StateType;
        public List<SerializableTransition> Transitions = new();
    }
}
