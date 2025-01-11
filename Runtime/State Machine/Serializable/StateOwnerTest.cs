using UnityEngine;
using Vapor.StateMachines;

namespace Vapor.StateMachines
{
    public class StateOwnerTest : MonoBehaviour, IStateOwner
    {
        public bool TransitionTest(Transition transition)
        {
            return true;
        }

        public bool TransitionTestOther()
        {
            return true;
        }

        public float DurationTest()
        {
            return 0f;
        }
    }
}
