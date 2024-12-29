using System.Collections.Generic;
using UnityEngine;

namespace Vapor.StateMachines
{
    [System.Serializable]
    public class SerializableState
    {
        public string Name;
        public List<SerializableTransition> Transitions;
    }
}
