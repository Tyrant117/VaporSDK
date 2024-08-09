using System;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace VaporEditor.Graphs
{
    public class BlueprintPort : Port
    {
        public static StyleSheet styleSheet;

        protected BlueprintPort(Orientation portOrientation, Direction portDirection, Capacity portCapacity, Type type) : base(portOrientation, portDirection, portCapacity, type)
        {
            styleSheet = styleSheet != null ? styleSheet : Resources.Load<StyleSheet>("Styles/ShaderPort");
            styleSheets.Add(styleSheet);
        }
    }
}
