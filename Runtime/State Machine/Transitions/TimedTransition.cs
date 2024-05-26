using System;

namespace Vapor.StateMachine
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

        protected TimedTransition(int from, int to, int desire, Func<float> durationEvaluator, Func<Transition, bool> condition) : base(from, to, desire, true, condition)
        {
            Timer = new Timer();
            _durationEvaluator = durationEvaluator;
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
            return new TimedTransition(From, To, Desire, _durationEvaluator, TimerComplete).ShouldForceTransition(ForceTransition);
        }
    }
}
