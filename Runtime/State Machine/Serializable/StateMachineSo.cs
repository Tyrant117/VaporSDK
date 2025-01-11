using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using UnityEngine;
using UnityEngine.Assertions;
using Vapor.Inspector;

namespace Vapor.StateMachines
{
    [CreateAssetMenu(fileName = "StateMachineSo", menuName = "Scriptable Objects/StateMachineSo")]
    public class StateMachineSo : VaporScriptableObject
    {
        [TypeSelector(TypeSelectorAttribute.T.Subclass, typeof(IStateOwner))]
        public string OwnerType;

        public List<SerializableState> States = new();
        public List<SerializableTransition> GlobalTransitions = new();

        [SuppressMessage("ReSharper", "PossibleNullReferenceException")]
        [SuppressMessage("ReSharper", "SuspiciousTypeConversion.Global")]
        public bool Build(IStateOwner owner, out StateMachine machine)
        {
            machine = new StateMachine(name);
            if (OwnerType.EmptyOrNull())
            {
                return false;
            }

            foreach (var serializableState in States)
            {
                State state = null;
                switch (serializableState.StateType)
                {
                    case SerializableState.Type.State:
                        state = new State(serializableState.Name, serializableState.CanExitInstantly, serializableState.CanTransitionToSelf);
                        break;
                    case SerializableState.Type.CoroutineState:
                        Assert.IsTrue(owner is MonoBehaviour, "Owner must be MonoBehaviour when creating CoroutineStates");
                        state = new CoroutineState((MonoBehaviour)owner, serializableState.Name, serializableState.CanExitInstantly);
                        break;
                    case SerializableState.Type.AsyncState:
                        throw new NotImplementedException("Async state is not implemented");
                }

                machine.AddState(state);

                foreach (var serializableTransition in serializableState.Transitions)
                {
                    switch (serializableTransition.TransitionType)
                    {
                        case SerializableTransition.Type.WaitForTrue:
                        {
                            var @delegate = (Func<Transition, bool>)Type.GetType(OwnerType)
                                .GetMethod(serializableTransition.MethodEvaluatorName)
                                .CreateDelegate(typeof(Func<Transition, bool>), owner);
                            var transition = new Transition(serializableTransition.FromStateName, serializableTransition.ToStateName, serializableTransition.Desire, @delegate);
                            machine.AddTransition(transition);
                        }
                            break;
                        case SerializableTransition.Type.TimedTransition:
                        {
                            var @delegate = (Func<float>)Type.GetType(OwnerType)
                                .GetMethod(serializableTransition.MethodEvaluatorName)
                                .CreateDelegate(typeof(Func<float>), owner);
                            var transition = new TimedTransition(serializableTransition.FromStateName, serializableTransition.ToStateName, serializableTransition.Desire, @delegate);
                            machine.AddTransition(transition);
                        }
                            break;
                        case SerializableTransition.Type.WaitForCoroutine:
                        {
                            if (state is CoroutineState coroutineState)
                            {
                                var transition = new CoroutineStateCompleteTransition(serializableTransition.FromStateName, serializableTransition.ToStateName, serializableTransition.Desire,
                                    coroutineState);
                                machine.AddTransition(transition);
                            }
                        }
                            break;
                    }
                }
            }

            foreach (var globalTransition in GlobalTransitions)
            {
                switch (globalTransition.TransitionType)
                {
                    case SerializableTransition.Type.WaitForTrue:
                    {
                        var @delegate = (Func<Transition, bool>)Type.GetType(OwnerType)
                            .GetMethod(globalTransition.MethodEvaluatorName)
                            .CreateDelegate(typeof(Func<Transition, bool>), owner);
                        var transition = new Transition(string.Empty, globalTransition.ToStateName, globalTransition.Desire, @delegate);
                        machine.AddTransitionFromAny(transition);
                    }
                        break;
                    case SerializableTransition.Type.TimedTransition:
                    {
                        var @delegate = (Func<float>)Type.GetType(OwnerType)
                            .GetMethod(globalTransition.MethodEvaluatorName)
                            .CreateDelegate(typeof(Func<float>), owner);
                        var transition = new TimedTransition(string.Empty, globalTransition.ToStateName, globalTransition.Desire, @delegate);
                        machine.AddTransitionFromAny(transition);
                    }
                        break;
                    case SerializableTransition.Type.WaitForCoroutine:
                    {
                        var state = machine.GetState<State>(globalTransition.FromStateName);
                        if (state is CoroutineState coroutineState)
                        {
                            var transition = new CoroutineStateCompleteTransition(string.Empty, globalTransition.ToStateName, globalTransition.Desire,
                                coroutineState);
                            machine.AddTransitionFromAny(transition);
                        }
                    }
                        break;
                }
            }

            return true;
        }
    }
}
