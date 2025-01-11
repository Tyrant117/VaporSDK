using System;

namespace Vapor.StateMachines
{
    public class TimedTransition : Transition
    {
        protected Timer Timer;
        protected float Duration;

        private readonly Func<float> _durationEvaluator;

        public TimedTransition(string from, string to, int desire, Func<float> durationEvaluator) : base(from, to, desire)
        {
            Timer = new Timer();
            _durationEvaluator = durationEvaluator;
            Condition = TimerComplete;
        }

        public TimedTransition(State from, State to, int desire, Func<float> durationEvaluator) : base(from, to, desire)
        {
            Timer = new Timer();
            _durationEvaluator = durationEvaluator;
            Condition = TimerComplete;
        }

        private TimedTransition(int from, int to, int desire, Func<float> durationEvaluator) : base(from, to, desire, true, null)
        {
            Timer = new Timer();
            _durationEvaluator = durationEvaluator;
            Condition = TimerComplete;
        }

        private bool TimerComplete(Transition thisT)
        {
            return Timer.IsOver(Duration);
        }

        public override void OnEnter()
        {
            Timer.Reset();
            Duration = _durationEvaluator.Invoke();
        }        

        public override Transition Reverse()
        {
            return new TimedTransition(To, From, Desire, _durationEvaluator).ShouldForceTransition(ForceTransition);
        }
    }
}
