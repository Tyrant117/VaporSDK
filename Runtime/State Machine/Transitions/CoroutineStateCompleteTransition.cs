using System;

namespace Vapor.StateMachines
{
    /// <summary>
    /// A transition that waits until the coroutine running in a <see cref="CoroutineState"/> is complete then returns true.
    /// </summary>
    public class CoroutineStateCompleteTransition : Transition
    {
        private readonly CoroutineState _watchingState;

        public CoroutineStateCompleteTransition(string from, string to, int desire, CoroutineState state) : base(from, to, desire)
        {
            _watchingState = state;
            Condition = CoroutineComplete;
        }

        public CoroutineStateCompleteTransition(State from, State to, int desire, CoroutineState state) : base(from, to, desire)
        {
            _watchingState = state;
            Condition = CoroutineComplete;
        }

        private CoroutineStateCompleteTransition(int from, int to, int desire, Func<Transition, bool> condition, CoroutineState state) : base(from, to, desire, true, condition)
        {
            _watchingState = state;
        }

        private bool CoroutineComplete(Transition t)
        {
            return _watchingState.CoroutineIsComplete;
        }

        public override Transition Reverse()
        {
            return new CoroutineStateCompleteTransition(To, From, Desire, CoroutineComplete, _watchingState).ShouldForceTransition(ForceTransition);
        }
    }
}
