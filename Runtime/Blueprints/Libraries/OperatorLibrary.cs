using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
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

    public static class MethodDelegateHelper
    {
        private static readonly Dictionary<MethodInfo, Delegate> s_GetDelegateCache = new();
        
        public static Delegate GetDelegateForMethod(MethodInfo mi)
        {
            if (s_GetDelegateCache.TryGetValue(mi, out Delegate cachedDelegate))
            {
                return cachedDelegate;
            }

            cachedDelegate = CreateDelegateForMethod(mi);
            s_GetDelegateCache[mi] = cachedDelegate;
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
    public static class OperatorLibrary
    {
        [BlueprintCallable(category: "Operators/Arithmetic", synonyms: new [] { "+" }), BlueprintPure]
        public static double Add(
            [BlueprintParam(wildcardTypes: new[]{typeof(float), typeof(double), typeof(int), typeof(long)})] double a,
            [BlueprintParam(wildcardTypes: new[]{typeof(float), typeof(double), typeof(int), typeof(long)})] double b)
        {
            return a + b;
        }
        
        [BlueprintCallable(category: "Operators/Arithmetic", synonyms: new [] { "-" }), BlueprintPure]
        public static double Subtract(
            [BlueprintParam(wildcardTypes: new[]{typeof(float), typeof(double), typeof(int), typeof(long)})] double a,
            [BlueprintParam(wildcardTypes: new[]{typeof(float), typeof(double), typeof(int), typeof(long)})] double b)
        {
            return a - b;
        }
        
        [BlueprintCallable(category: "Operators/Arithmetic", synonyms: new [] { "*" }), BlueprintPure]
        public static double Multiply(
            [BlueprintParam(wildcardTypes: new[]{typeof(float), typeof(double), typeof(int), typeof(long)})] double a,
            [BlueprintParam(wildcardTypes: new[]{typeof(float), typeof(double), typeof(int), typeof(long)})] double b)
        {
            return a * b;
        }
        
        [BlueprintCallable(category: "Operators/Arithmetic", synonyms: new [] { "/" }), BlueprintPure]
        public static double Divide(
            [BlueprintParam(wildcardTypes: new[]{typeof(float), typeof(double), typeof(int), typeof(long)})] double a,
            [BlueprintParam(wildcardTypes: new[]{typeof(float), typeof(double), typeof(int), typeof(long)})] double b)
        {
            return a / b;
        }
        
        [BlueprintCallable(category: "Operators/Arithmetic", synonyms: new [] { "%" }), BlueprintPure]
        public static double Modulus(
            [BlueprintParam(wildcardTypes: new[]{typeof(float), typeof(double), typeof(int), typeof(long)})] double a,
            [BlueprintParam(wildcardTypes: new[]{typeof(float), typeof(double), typeof(int), typeof(long)})] double b)
        {
            return a % b;
        }
        
        [BlueprintCallable(nodeName: "And",category: "Operators/Logic", synonyms: new [] { "&&" }), BlueprintPure]
        public static bool And(bool a, bool b) => a && b;
        
        [BlueprintCallable(nodeName: "Or",category: "Operators/Logic", synonyms: new [] { "||" }), BlueprintPure]
        public static bool Or(bool a, bool b) => a || b;

        [BlueprintCallable(category: "Operators/Logic", synonyms: new[] { "!" }), BlueprintPure]
        public static bool Negate(bool a) => !a;
        
        [BlueprintCallable(nodeName: "Logical And",category: "Operators/Logic", synonyms: new [] { "&" }), BlueprintPure]
        public static bool LogicalAnd(bool a, bool b) => a & b;
        
        [BlueprintCallable(nodeName: "Logical Or",category: "Operators/Logic", synonyms: new [] { "|" }), BlueprintPure]
        public static bool LogicalOr(bool a, bool b) => a | b;
        
        [BlueprintCallable(nodeName: "Logical XOr",category: "Operators/Logic", synonyms: new [] { "^" }), BlueprintPure]
        public static bool LogicalXOr(bool a, bool b) => a ^ b;
    }

    // [BlueprintLibrary]
    // public static class ObjectLibrary
    // {
    //     [BlueprintCallable(nodeName: "CastTo", category: "Utilities"), BlueprintPure]
    //     public static object Cast(object @object, Type type)
    //     {
    //         return TypeUtility.CastToType(@object, type);
    //     }
    // }
}
