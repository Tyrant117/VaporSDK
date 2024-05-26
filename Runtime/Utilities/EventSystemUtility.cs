using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Vapor
{
    public static class EventSystemUtility
    {
        /// <summary>
        /// Checks if the pointer is over a GUI element
        /// </summary>
        /// <returns>True if the pointer is over a GUI element, otherwise false</returns>
        public static bool IsPointerOverGUI()
        {
            return EventSystem.current && EventSystem.current.IsPointerOverGameObject();
        }
    }
}
