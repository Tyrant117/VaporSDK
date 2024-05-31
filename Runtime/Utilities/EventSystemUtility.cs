using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;

namespace Vapor
{
    public static class EventSystemUtility
    {
        private static InputSystemUIInputModule s_Module;

        /// <summary>
        /// Checks if the pointer is over a GUI element
        /// </summary>
        /// <returns>True if the pointer is over a GUI element, otherwise false</returns>
        public static bool IsPointerOverGUI()
        {
            return EventSystem.current && EventSystem.current.IsPointerOverGameObject();
        }

        public static bool IsPointerOverGUIAction()
        {
            if (!EventSystem.current)
                return false;

            if (!s_Module)
                s_Module = (InputSystemUIInputModule)EventSystem.current.currentInputModule;

            return s_Module.GetLastRaycastResult(Mouse.current.deviceId).isValid;
        }
    }
}
