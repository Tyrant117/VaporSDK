using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Vapor.VisualScripting
{
    [AttributeUsage(AttributeTargets.Class)]
    public class BlueprintLibraryAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.Method)]
    public class BlueprintCallableAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.Method)]
    public class BlueprintPureAttribute : Attribute { }
    
    [AttributeUsage(AttributeTargets.Parameter)]
    public class BlueprintPropertyAttribute : Attribute { }
    
    [BlueprintLibrary]
    public static class MathLibrary
    {
        // Delegate cache to avoid repetitive reflection calls
        private static readonly Dictionary<MethodInfo, Delegate> DelegateCache = new();
        
        [BlueprintPure]
        public static double Add(double a, double b)
        {
            return a + b;
        }

        [BlueprintCallable]
        public static void TryGetStat([BlueprintProperty] ushort key, [BlueprintProperty] int queryType, out double baseValue, out double modifiedValue)
        {
            baseValue = 0;
            modifiedValue = 0;
        }

        public static void Cache()
        {
            var bpl = TypeCache.GetTypesWithAttribute<BlueprintLibraryAttribute>();
            foreach (var l in bpl)
            {
                var mis = l.GetMethods(BindingFlags.Public | BindingFlags.Static);
                foreach (var mi in mis)
                {
                    var pureAttribute = mi.GetCustomAttribute<BlueprintPureAttribute>();
                    if (pureAttribute == null)
                    {
                        continue;
                    }

                    if (DelegateCache.TryGetValue(mi, out Delegate cachedDelegate))
                    {
                        continue;
                    }

                    cachedDelegate = CreateDelegateForMethod(mi);
                    DelegateCache[mi] = cachedDelegate;
                }
            }
            
            var mc = TypeCache.GetMethodsWithAttribute<BlueprintCallableAttribute>();
            foreach (var m in mc)
            {
                if (!DelegateCache.TryGetValue(m, out Delegate cachedDelegate))
                {
                    cachedDelegate = CreateDelegateForMethod(m);
                    DelegateCache[m] = cachedDelegate;
                }
            }
        }
        
        // Optimized delegate creation using expression trees
        private static Delegate CreateDelegateForMethod(MethodInfo methodInfo)
        {
            // Get parameters for the delegate
            var parameters = methodInfo.GetParameters();
            var parameterExpressions = new ParameterExpression[parameters.Length];

            for (int i = 0; i < parameters.Length; i++)
            {
                parameterExpressions[i] = Expression.Parameter(parameters[i].ParameterType, parameters[i].Name);
            }

            // Create a call expression
            var callExpression = Expression.Call(methodInfo, parameterExpressions);

            // Compile to a delegate
            var lambda = Expression.Lambda(callExpression, parameterExpressions);
            return lambda.Compile();
        }
    }

    public class DemoNode
    {
        public DemoNode NextNode;
        
        public Delegate Function;
        public Dictionary<string, (DemoNode, string, Type)> InPorts = new();
        public Dictionary<string, object> CustomData = new();
        
        public readonly List<(string, object)> InPortValues = new();
        public readonly List<(string, object)> OutPortValues = new();

        public bool Invoked;
        
        public void Reset()
        {
            Invoked = false;
        }

        public void Invoke()
        {
            // Validate In Nodes
            int idx = 0;
            foreach (var node in InPorts)
            {
                node.Value.Item1.Invoke();
                var value = node.Value.Item1.GetValue(node.Value.Item2);
                value = Convert.ChangeType(value, node.Value.Item3);
                if (InPortValues.IsValidIndex(idx))
                {
                    InPortValues[idx] = (node.Key, value);
                }
                else
                {
                    InPortValues.Add((node.Key, value));
                }

                idx++;
            }

            if (Function != null)
            {
                int returnOne = Function.Method.ReturnType == typeof(void) ? 0 : -1;
                // if the method isn't static the first parameter needs to be the assigned owner
                // then custom data
                // then the other in port values
                // then the out ports
                object[] parameters = new object[InPortValues.Count + OutPortValues.Count + returnOne];
                for (var i = 0; i < InPortValues.Count; i++)
                {
                    var pair = InPortValues[i];
                    parameters[i] = pair.Item2;
                }

                if (returnOne == 0)
                {
                    Function.DynamicInvoke(parameters);

                    for (var i = 0; i < OutPortValues.Count; i++)
                    {
                        var portName = OutPortValues[i].Item1;
                        OutPortValues[i] = (portName, parameters[InPortValues.Count + i]);
                    }
                }
                else
                {
                    var outPort = OutPortValues[0].Item1;
                    var outVal = Function.DynamicInvoke(parameters);
                    OutPortValues[0] = (outPort, outVal);

                    for (var i = 1; i < OutPortValues.Count; i++)
                    {
                        var portName = OutPortValues[i].Item1;
                        OutPortValues[i] = (portName, parameters[InPortValues.Count + i - 1]);
                    }
                }
            }

            if (Invoked)
            {
                return;
            }

            Invoked = true;
            NextNode?.Invoke();
        }

        public object GetValue(string port)
        {
            return OutPortValues.FirstOrDefault(x => x.Item1 == port).Item2;
        }
        
        
    }
}
