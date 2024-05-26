using System;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Vapor
{
    public static class InputActionExtensions
    {
#if ENABLE_INPUT_SYSTEM
        public static InputAction WithPerformed(this InputAction action, Action callback)
        {
            action.performed += _ => callback.Invoke();
            return action;
        }

        public static InputAction WithPerformed<T>(this InputAction action, Action<T> callback, T value)
        {
            action.performed += _ => callback.Invoke(value);
            return action;
        }

        public static InputAction WithPerformedRead<T>(this InputAction action, Action<T> callback) where T : struct
        {
            action.performed += context => callback.Invoke(context.ReadValue<T>());
            return action;
        }

        public static InputAction WithCanceled(this InputAction action, Action callback)
        {
            action.canceled += _ => callback.Invoke();
            return action;
        }

        public static InputAction WithCanceled<T>(this InputAction action, Action<T> callback, T value)
        {
            action.canceled += _ => callback.Invoke(value);
            return action;
        }

        public static InputAction WithCanceledRead<T>(this InputAction action, Action<T> callback) where T : struct
        {
            action.canceled += context => callback.Invoke(context.ReadValue<T>());
            return action;
        }

        public static InputAction WithPerformedAndCanceled(this InputAction action, Action callback)
        {
            action.performed += _ => callback.Invoke();
            action.canceled += _ => callback.Invoke();
            return action;
        }

        public static InputAction WithPerformedAndCanceled<T>(this InputAction action, Action<T> callback, T performed, T canceled)
        {
            action.performed += _ => callback.Invoke(performed);
            action.canceled += _ => callback.Invoke(canceled);
            return action;
        }

        public static InputAction WithPerformedAndCanceledRead<T>(this InputAction action, Action<T> callback) where T : struct
        {
            action.performed += context => callback.Invoke(context.ReadValue<T>());
            action.canceled += context => callback.Invoke(context.ReadValue<T>());
            return action;
        }
#endif
    }
}
