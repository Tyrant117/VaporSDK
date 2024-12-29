namespace Vapor.StateMachines
{
    public static class StateMachineExtensions
    {
        #region - Transitions -
        /// <summary>
        /// Adds two transitions:
        /// If the condition of the transition instance is true, it transitions from the "from"
        /// state to the "to" state. Otherwise, it performs a transition in the opposite direction,
        /// i.e. from "to" to "from".
        /// </summary>
        /// <remarks>
        /// Internally the same transition instance will be used for both transitions
        /// by wrapping it in a ReverseTransition.
        /// </remarks>
        public static void AddTwoWayTransition(this StateMachine fsm, Transition transition)
		{
			fsm.AddTransition(transition);
			var reverse = transition.Reverse();
			fsm.AddTransition(reverse);
		}

        /// <summary>
		/// Adds two transitions that are only checked when the specified trigger is activated:
		/// If the condition of the transition instance is true, it transitions from the "from"
		/// state to the "to" state. Otherwise, it performs a transition in the opposite direction,
		/// i.e. from "to" to "from".
		/// </summary>
		/// <remarks>
		/// Internally the same transition instance will be used for both transitions
		/// by wrapping it in a ReverseTransition.
		/// </remarks>
		public static void AddTwoWayTriggerTransition(this StateMachine fsm, int trigger, Transition transition)
        {
            fsm.AddTriggerTransition(trigger, transition);
            var reverse = transition.Reverse();
            fsm.AddTriggerTransition(trigger, reverse);
        }
        #endregion
    }
}
