using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Vapor.StateMachines
{
    public class CoroutineState : State
    {
        private readonly Func<CoroutineState, IEnumerator> OnCoroutineUpdated;
        private readonly MonoBehaviour Runner;
        private Coroutine Routine;
        private readonly bool ExitAfterCoroutine;
        private readonly int Iterations;

        public bool CoroutineIsComplete { get; private set; }
        private int IterationCount;

        public CoroutineState(MonoBehaviour runner, string name, bool canExitInstantly, Func<CoroutineState, IEnumerator> updated = null) : base(name, canExitInstantly)
        {
            Runner = runner;
            OnCoroutineUpdated = updated;
        }
        public CoroutineState(MonoBehaviour runner, string name, bool canExitInstantly, bool exitAfterCoroutine, int iterations, Func<CoroutineState, IEnumerator> updated = null) 
            : base(name, canExitInstantly)
        {
            Runner = runner;
            ExitAfterCoroutine = exitAfterCoroutine;
            Iterations = iterations;
            OnCoroutineUpdated = updated;
        }

        public override void OnEnter()
        {
            Routine = null;
            CoroutineIsComplete = false;
            IterationCount = 0;
            base.OnEnter();
        }

        public override void OnUpdate()
        {
            if (Routine == null && OnCoroutineUpdated != null)
            {
                Routine = ExitAfterCoroutine ? Runner.StartCoroutine(RunCoroutine()) : Runner.StartCoroutine(LoopCoroutine());
            }
        }

        private IEnumerator RunCoroutine()
        {
            var routine = OnCoroutineUpdated(this);
            for (var i = 0; i < Iterations; i++)
            {
                // This checks if the routine needs at least one frame to execute.
                // If not, LoopCoroutine will wait 1 frame to avoid an infinite
                // loop which will crash Unity
                yield return routine.MoveNext() ? routine.Current : null;

                // Iterate from the onLogic coroutine until it is depleted
                while (routine.MoveNext())
                {
                    yield return routine.Current;
                }
                IterationCount++;                
                if (IterationCount < Iterations)
                {
                    // Restart the onLogic coroutine
                    routine = OnCoroutineUpdated(this);
                }
            }
            CoroutineIsComplete = true;
        }

        private IEnumerator LoopCoroutine()
        {
            var routine = OnCoroutineUpdated(this);
            while (true)
            {

                // This checks if the routine needs at least one frame to execute.
                // If not, LoopCoroutine will wait 1 frame to avoid an infinite
                // loop which will crash Unity
                yield return routine.MoveNext() ? routine.Current : null;

                // Iterate from the onLogic coroutine until it is depleted
                while (routine.MoveNext())
                {
                    yield return routine.Current;
                }

                // Restart the onLogic coroutine
                routine = OnCoroutineUpdated(this);
            }
            // ReSharper disable once IteratorNeverReturns
        }

        public override void OnExit(Transition transition)
        {
            if (Routine != null)
            {
                Runner.StopCoroutine(Routine);
                Routine = null;
            }
            CoroutineIsComplete = false;
            IterationCount = 0;
            base.OnExit(transition);
        }
    }
}
