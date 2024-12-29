using System;
using System.Collections.Generic;
using UnityEngine;
using Vapor;

namespace Vapor.StateMachines
{
    public class State
    {
        private static int? _emptyState;
        public static int EmptyState
        {
            get
            {
                _emptyState ??= "".GetStableHashU16();
                return _emptyState.Value;
            }
        }

        /// <summary>
        /// The name of the state
        /// </summary>
        public string Name { get; }
        /// <summary>
        /// The full path to this state.
        /// e.g. "Movement/Walking/WalkForward"
        /// </summary>
        public string Path { get; }
        /// <summary>
        /// The ID of the state
        /// </summary>
        public ushort Id { get; }
        /// <summary>
        /// If true, the state will immediately test all outgoing transitions before calling OnUpdate.
        /// </summary>
        public bool CanExitInstantly { get; }
        /// <summary>
        /// If true, this state can trigger transitions that will restart the state calling OnExit then OnEnter
        /// </summary>
        public bool CanTransitionToSelf { get; }

        /// <summary>
        /// The state machine this state belongs to.
        /// </summary>
        public IStateMachine StateMachine { get; set; }

        /// <summary>
        /// The current owner of this state.
        /// </summary>
        public IStateOwner CurrentOwner { get; private set; }

        /// <summary>
        /// The internal timer that tracks the time since last entry to the state
        /// </summary>
        public Timer Timer { get; }

        /// <summary>
        /// True if the state is playing.
        /// </summary>
        public bool IsPlaying { get; protected set; }

        protected event Action<State> Entered;
        protected event Action<State> Updated;
        protected event Action<State, Transition> Exited;
        protected Func<State, bool> CanExit;
        protected Dictionary<int, Delegate> ActionsByEventMap;

        public State(string name, bool canExitInstantly, bool canTransitionToSelf = false)
        {
            Name = name;
            Id = name.GetStableHashU16();
            CanExitInstantly = canExitInstantly;
            CanTransitionToSelf = canTransitionToSelf;
            Timer = new Timer();
            CanExit = CanAlwaysExit;
        }

        public State WithEntered(Action<State> entered)
        {
            Entered += entered;
            return this;
        }

        public State WithUpdated(Action<State> updated)
        {
            Updated += updated;
            return this;
        }

        public State WithExited(Action<State, Transition> exited)
        {
            Exited += exited;
            return this;
        }

        public State WithCanExitRequest(Func<State, bool> canExit)
        {
            CanExit = canExit;
            return this;
        }

        public State WithOwner(IStateOwner owner)
        {
            CurrentOwner = owner;
            return this;
        }

        public virtual void OnEnable()
        {

        }

        /// <summary>
        /// Called once when the state is entered. Always called after OnExit of the previous state.
        /// </summary>
        public virtual void OnEnter()
        {
            IsPlaying = true;
            Timer.Reset();

            Entered?.Invoke(this);
        }

        /// <summary>
        /// Called everytime the parent StateMachine calls Update.
        /// </summary>
        public virtual void OnUpdate()
        {
            Updated?.Invoke(this);
        }

        /// <summary>
        /// Called when the state is exited.
        /// </summary>
        /// <param name="transition"></param>
        public virtual void OnExit(Transition transition)
        {
            IsPlaying = false;
            Exited?.Invoke(this, transition);
        }

        /// <summary>
        /// Called when a state transition from this state should happen. If the state should not transition immediatly this method can be overriden.
        /// Call <see cref="StateMachine.StateCanExit(Transition)"/> when the transition should happen.
        /// </summary>
        /// <param name="transition"></param>
        public virtual void OnExitRequest(Transition transition = null)
        {
            if (CanExit.Invoke(this))
            {
                StateMachine.StateCanExit(transition);
            }
        }    

        #region - User Defined Actions -
        protected void AddUserDefinedAction(int actionID, Delegate action)
        {
            ActionsByEventMap ??= new Dictionary<int, Delegate>();
            ActionsByEventMap[actionID] = action;
        }

        /// <summary>
        /// Adds an action that can be called with OnAction(). Actions are like the builtin events
        /// OnEnter / OnLogic / ... but are defined by the user.
        /// </summary>
        /// <param name="actionID">ID of the action</param>
        /// <param name="action">Function that should be called when the action is run</param>
        /// <returns>Itself</returns>
        public State WithUserDefinedAction(int actionID, Action action)
        {
            AddUserDefinedAction(actionID, action);
            return this;
        }

        /// <summary>
        /// Adds an action that can be called with OnAction{T}(). This overload allows you to
        /// run a function that takes one data parameter.
        /// Actions are like the builtin events OnEnter / OnLogic / ... but are defined by the user.
        /// </summary>
        /// <param name="actionID">ID of the action</param>
        /// <param name="action">Function that should be called when the action is run</param>
        /// <typeparam name="TData">Data type of the parameter of the function</typeparam>
        /// <returns>Itself</returns>
        public State WithUserDefinedAction<TData>(int actionID, Action<TData> action)
        {
            AddUserDefinedAction(actionID, action);
            return this;
        }

        protected TTarget TryGetAndCastUserDefinedAction<TTarget>(int actionID) where TTarget : Delegate
        {
            if (!ActionsByEventMap.TryGetValue(actionID, out var action))
            {
                return null;
            }

            if (action is TTarget target)
            {
                return target;
            }

            Debug.LogError(StateMachineExceptions.ActionTypeMismatch(typeof(TTarget), action));
            return null;
        }

        /// <summary>
        /// Runs an action with the given name.
        /// If the action is not defined / hasn't been added, nothing will happen.
        /// </summary>
        /// <param name="actionID">Name of the action</param>
        public void OnUserDefinedAction(int actionID) => TryGetAndCastUserDefinedAction<Action>(actionID)?.Invoke();

        /// <summary>
        /// Runs an action with a given name and lets you pass in one parameter to the action function.
        /// If the action is not defined / hasn't been added, nothing will happen.
        /// </summary>
        /// <param name="actionID">Name of the action</param>
        /// <param name="data">Data to pass as the first parameter to the action</param>
        /// <typeparam name="TData">Type of the data parameter</typeparam>
        public void OnUserDefinedAction<TData>(int actionID, TData data) => TryGetAndCastUserDefinedAction<Action<TData>>(actionID)?.Invoke(data);
        #endregion

        #region - Helpers -
        /// <summary>
        /// Helper method to always return true when trying to exit. Saves a null check on the CanExit Func.
        /// </summary>
        /// <returns>True</returns>
        private bool CanAlwaysExit(State thisState) => true;
        #endregion
    }
}
