using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace VaporEditor.GraphTools.Math
{
    public abstract class AbstractMathNode
    {
        public string Name { get; set; }

        public abstract MethodInfo GetFunctionToConvert();
    }
}
