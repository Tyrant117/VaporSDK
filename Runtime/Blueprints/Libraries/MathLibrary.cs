using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;
using Vapor.Keys;

namespace Vapor.Blueprints
{
    public static class FieldDelegateHelper
    {
        private static readonly Dictionary<FieldInfo, Delegate> s_GetDelegateCache = new();
        private static readonly Dictionary<FieldInfo, Delegate> s_SetDelegateCache = new();

        public static Delegate GetDelegateForFieldGetter(FieldInfo fieldInfo)
        {
            if (s_GetDelegateCache.TryGetValue(fieldInfo, out Delegate cachedDelegate))
            {
                return cachedDelegate;
            }

            cachedDelegate = CreateFieldGetterDelegate(fieldInfo);
            s_GetDelegateCache[fieldInfo] = cachedDelegate;
            return cachedDelegate;
        }

        public static Delegate GetDelegateForFieldSetter(FieldInfo fieldInfo)
        {
            if (s_SetDelegateCache.TryGetValue(fieldInfo, out Delegate cachedDelegate))
            {
                return cachedDelegate;
            }

            cachedDelegate = CreateFieldSetterDelegate(fieldInfo);
            s_SetDelegateCache[fieldInfo] = cachedDelegate;
            return cachedDelegate;
        }

        private static Delegate CreateFieldGetterDelegate(FieldInfo fieldInfo)
        {
            ParameterExpression instanceParam = null;
            Expression fieldAccess;

            if (fieldInfo.IsStatic)
            {
                // ✅ Static field: No instance required
                fieldAccess = Expression.Field(null, fieldInfo);
            }
            else
            {
                // ✅ Instance field: Requires an instance parameter
                instanceParam = Expression.Parameter(typeof(object), "instance");
                Assert.IsTrue(fieldInfo.DeclaringType != null, $"{fieldInfo} Declaring Type Cannot Be Null.");
                Expression convertedInstance = Expression.Convert(instanceParam, fieldInfo.DeclaringType);
                fieldAccess = Expression.Field(convertedInstance, fieldInfo);
            }

            // ✅ Convert field access result to `object`
            Expression convertedFieldAccess = Expression.Convert(fieldAccess, typeof(object));

            // ✅ Create and compile lambda expression
            if (fieldInfo.IsStatic)
            {
                var lambda = Expression.Lambda<Func<object>>(convertedFieldAccess);
                return lambda.Compile();
            }
            else
            {
                var lambda = Expression.Lambda<Func<object, object>>(convertedFieldAccess, instanceParam);
                return lambda.Compile();
            }
        }

        private static Delegate CreateFieldSetterDelegate(FieldInfo fieldInfo)
        {
            ParameterExpression valueParam = Expression.Parameter(typeof(object), "value");
            Expression convertedValue = Expression.Convert(valueParam, fieldInfo.FieldType);
            Expression fieldAssign;

            if (fieldInfo.IsStatic)
            {
                // ✅ Static field: No instance required
                fieldAssign = Expression.Assign(Expression.Field(null, fieldInfo), convertedValue);
                var lambda = Expression.Lambda<Action<object>>(fieldAssign, valueParam);
                return lambda.Compile();
            }
            else
            {
                // ✅ Instance field: Requires an instance parameter
                var instanceParam = Expression.Parameter(typeof(object), "instance");
                Assert.IsTrue(fieldInfo.DeclaringType != null, $"{fieldInfo} Declaring Type Cannot Be Null.");
                Expression convertedInstance = Expression.Convert(instanceParam, fieldInfo.DeclaringType);
                fieldAssign = Expression.Assign(Expression.Field(convertedInstance, fieldInfo), convertedValue);

                var lambda = Expression.Lambda<Action<object, object>>(fieldAssign, instanceParam, valueParam);
                return lambda.Compile();
            }
        }
    }

    [BlueprintLibrary]
    public static class MathLibrary
    {
        // Delegate cache to avoid repetitive reflection calls
        private static readonly Dictionary<MethodInfo, Delegate> DelegateCache = new();
        
        [BlueprintPure(category: "Math", synonyms: new [] { "+" })]
        public static double Add(double a, double b)
        {
            return a + b;
        }
        
        [BlueprintPure(category: "Math")]
        public static double Subtract(double a, double b)
        {
            return a - b;
        }
        
        [BlueprintPure(category: "Math")]
        public static double Multiply(double a, double b)
        {
            return a * b;
        }
        
        [BlueprintPure(category: "Math")]
        public static double Divide(double a, double b)
        {
            return a / b;
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

        public static Delegate GetDelegateForMethod(MethodInfo mi)
        {
            if (DelegateCache.TryGetValue(mi, out Delegate cachedDelegate))
            {
                return cachedDelegate;
            }

            cachedDelegate = CreateDelegateForMethod(mi);
            DelegateCache[mi] = cachedDelegate;
            return cachedDelegate;

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
            Expression callExpression;
            if (methodInfo.IsStatic)
            {
                // Static method: No instance needed
                callExpression = Expression.Call(methodInfo, parameterExpressions);
                
                // Compile to a delegate
                var lambda = Expression.Lambda(callExpression, parameterExpressions);
                return lambda.Compile();
            }
            else
            {
                // Instance method: Requires an instance
                var instanceParam = Expression.Parameter(typeof(object), "instance"); // ✅ Instance is now dynamic
                var convertedInstance = Expression.Convert(instanceParam, methodInfo.DeclaringType);
                callExpression = Expression.Call(convertedInstance, methodInfo, parameterExpressions);
                
                // Compile to a delegate
                var lambda = Expression.Lambda(callExpression, new[] { instanceParam }.Concat(parameterExpressions));
                return lambda.Compile();
            }
        }
    }

    [BlueprintLibrary]
    public static class LoggingLibrary
    {
        [BlueprintCallable(nodeName: "Log", category: "Logging")]
        public static void Log([BlueprintParam("Message")]string message)
        {
            Debug.Log(message);
        }
        
        [BlueprintCallable(nodeName: "Log Warning", category: "Logging")]
        public static void LogWarning([BlueprintParam("Message")] string message)
        {
            Debug.LogWarning(message);
        }
        
        [BlueprintCallable(nodeName: "Log Error", category: "Logging")]
        public static void LogError([BlueprintParam("Message")]string message)
        {
            Debug.LogError(message);
        }
    }

    [BlueprintLibrary]
    public static class ObjectLibrary
    {
        [BlueprintPure(nodeName: "GetType", category: "Object")]
        public static Type GetType_BP(object @object)
        {
            return @object.GetType();
        }

        [BlueprintPure(nodeName: "Cast", category: "Object")]
        public static object Cast(object @object, Type type)
        {
            if (@object == null)
            {
                if (type.IsClass || Nullable.GetUnderlyingType(type) != null)
                {
                    return null; // Null is a valid value for reference types and nullable types
                }
                throw new ArgumentNullException(nameof(@object), "Cannot cast null to a non-nullable value type.");
            }

            // Check if the object is already of the target type
            if (type.IsAssignableFrom(@object.GetType()))
            {
                return @object; // No casting needed
            }

            return Convert.ChangeType(@object, type);
        }

        [BlueprintPure(nodeName: "To String", category: "Object")]
        public static string ToString_BP(object @object)
        {
            return @object.ToString();
        }
    }

    [BlueprintLibrary]
    public static class ComponentLibrary
    {
        [BlueprintCallable(category: "Component")]
        public static bool TryGetComponent(Component owner, Type componentType, out Component component)
        {
            return owner.TryGetComponent(componentType, out component);
        }
        
        [BlueprintCallable(category: "Component")]
        public static Component GetComponent(Component owner, Type componentType)
        {
            return owner.GetComponent(componentType);
        }
        
        [BlueprintCallable(category: "Component")]
        public static Component GetComponentInParent(Component owner, Type componentType, bool includeInactive = false)
        {
            return owner.GetComponentInParent(componentType, includeInactive);
        }
        
        [BlueprintCallable(category: "Component")]
        public static Component GetComponentInChildren(Component owner, Type componentType, bool includeInactive = false)
        {
            return owner.GetComponentInChildren(componentType, includeInactive);
        }
        
        [BlueprintCallable(category: "Component")]
        public static Component[] GetComponents(Component owner, Type componentType)
        {
            return owner.GetComponents(componentType);
        }
        
        [BlueprintCallable(category: "Component")]
        public static Component[] GetComponentsInParent(Component owner, Type componentType, bool includeInactive = false)
        {
            return owner.GetComponentsInParent(componentType, includeInactive);
        }
        
        [BlueprintCallable(category: "Component")]
        public static Component[] GetComponentsInChildren(Component owner, Type componentType, bool includeInactive = false)
        {
            return owner.GetComponentsInChildren(componentType, includeInactive);
        }
    }

    public class ResourceCheckGraph
    {
        private const int ID = 232321;
        private readonly BlueprintFunctionGraph _graph;
        private readonly Action<IBlueprintGraph> _returnCallback;
        private readonly object[] _parameters;
        public ResourceCheckGraph(Action<IBlueprintGraph> returnCallback)
        {
            _graph = RuntimeDataStore<IBlueprintGraph>.Get<BlueprintFunctionGraph>(ID);
            _returnCallback = returnCallback;
            _parameters = new object[4];
        }

        public void Invoke(double a, double b, double c, double d)
        {
            _parameters[0] = a;
            _parameters[1] = b;
            _parameters[2] = c;
            _parameters[3] = d;
            _graph.Invoke(_parameters, _returnCallback);
        }
    }
}
