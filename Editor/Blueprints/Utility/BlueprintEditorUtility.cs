using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Vapor;
using VaporEditor.Inspector;

namespace VaporEditor
{
    public class BlueprintEditorUtility
    {
        public static readonly Dictionary<string, (Type, string)[]> UnityMessageMethods = new()
        {
            // Lifecycle
            { "Awake()", null },
            { "OnEnable()", null },
            { "Start()", null },
            { "FixedUpdate()", null },
            { "Update()", null },
            { "LateUpdate()", null },
            { "OnDisable()", null },
            { "OnDestroy()", null },

            // Coroutines
            { "OnApplicationPause(bool)", new[] { (typeof(bool), "pauseStatus") } },
            { "OnApplicationFocus(bool)", new[] { (typeof(bool), "hasFocus") } },
            { "OnApplicationQuit()", null },

            // Rendering
            { "OnPreRender()", null },
            { "OnRenderObject()", null },
            { "OnWillRenderObject()", null },
            { "OnPostRender()", null },
            { "OnRenderImage(RenderTexture, RenderTexture)", new[] { (typeof(RenderTexture), "source"), (typeof(RenderTexture), "destination") } },
            { "OnBecameVisible()", null },
            { "OnBecameInvisible()", null },

            // Physics - Collision
            { "OnCollisionEnter(Collision)", new[] { (typeof(Collision), "other") } },
            { "OnCollisionStay(Collision)", new[] { (typeof(Collision), "other") } },
            { "OnCollisionExit(Collision)", new[] { (typeof(Collision), "other") } },
            { "OnCollisionEnter2D(Collision2D)", new[] { (typeof(Collision2D), "other") } },
            { "OnCollisionStay2D(Collision2D)", new[] { (typeof(Collision2D), "other") } },
            { "OnCollisionExit2D(Collision2D)", new[] { (typeof(Collision2D), "other") } },


            // Physics - Trigger
            { "OnTriggerEnter(Collider)", new[] { (typeof(Collider), "other") } },
            { "OnTriggerStay(Collider)", new[] { (typeof(Collider), "other") } },
            { "OnTriggerExit(Collider)", new[] { (typeof(Collider), "other") } },
            { "OnTriggerEnter2D(Collider2D)", new[] { (typeof(Collider2D), "other") } },
            { "OnTriggerStay2D(Collider2D)", new[] { (typeof(Collider2D), "other") } },
            { "OnTriggerExit2D(Collider2D)", new[] { (typeof(Collider2D), "other") } },

            // Physics - Misc
            { "OnControllerColliderHit(ControllerColliderHit)", new[] { (typeof(ControllerColliderHit), "hit") } },
            { "OnJointBreak(float)", new[] { (typeof(float), "breakForce") } },
            { "OnJointBreak2D(Joint2D)", new[] { (typeof(Joint2D), "brokenJoint") } },
            { "OnParticleCollision(GameObject)", new[] { (typeof(GameObject), "other") } },

            // Input / Mouse
            { "OnMouseDown()", null },
            { "OnMouseUp()", null },
            { "OnMouseEnter()", null },
            { "OnMouseExit()", null },
            { "OnMouseOver()", null },
            { "OnMouseDrag()", null },

            // Animator
            { "OnAnimatorIK(int)", new[] { (typeof(int), "layerIndex") } },
            {
                "OnAnimatorMove()", null
            },

            // Audio
            {
                "OnAudioFilterRead(float[], int)", new[] { (typeof(float[]), "data"), (typeof(int), "channels") }
            },

            // Gizmos
            { "OnDrawGizmos()", null },
            { "OnDrawGizmosSelected()", null },

            // UI / Canvas
            { "OnCanvasGroupChanged()", null },
            { "OnRectTransformDimensionsChange()", null },

            // Editor / Utility
            { "Reset()", null },
            { "OnValidate()", null },
        };
        
        public static string FormatMethodName(MethodInfo method)
        {
            var displayName = method.IsGenericMethod ? $"{method.Name.Split('`')[0]}<{string.Join(",", method.GetGenericArguments().Select(FormatTypeName))}>" : method.Name;
            displayName = method.IsSpecialName ? displayName.ToTitleCase() : displayName;
            var parameters = method.GetParameters();
            string paramNames = parameters.Length > 0
                ? parameters.Select(pi => FormatTypeName(pi.ParameterType))
                    .Aggregate((a, b) => a + ", " + b)
                : string.Empty;
            return $"{displayName}({paramNames})";
        }

        public static string FormatTypeName(Type type)
        {
            if (type == null)
            {
                return "None";
            }
            
            string typeName = type.FullName switch
            {
                "System.Int32" => "int",
                "System.Boolean" => "bool",
                "System.Single" => "float",
                "System.Double" => "double",
                "System.Int16" => "short",
                "System.Int64" => "long",
                "System.Char" => "char",
                "System.Byte" => "byte",
                "System.SByte" => "sbyte",
                "System.UInt16" => "ushort",
                "System.UInt32" => "uint",
                "System.UInt64" => "ulong",
                "System.String" => "string",
                "System.Object" => "object",
                "System.Delegate" => "delegate",
                _ => type.Name // Fallback for custom types
            };

            var tn = type.IsGenericType ? $"{typeName.Split('`')[0]}<{string.Join(",", type.GetGenericArguments().Select(FormatTypeName))}>" : typeName;
            return tn;
        }
        
        public static ConstructorInfo GetConstructor(Type type, string constructorSignature)
        {
            if (constructorSignature == "Default(T)")
            {
                return null;
            }
            var constructors = type.GetConstructors(BindingFlags.Public | BindingFlags.Instance);
            return (from constructor in constructors let signature = FormatConstructorSignature(constructor) where signature.Equals(constructorSignature) select constructor).FirstOrDefault();
        }
        
        public static string FormatConstructorSignature(ConstructorInfo c)
        {
            // Get the parameter list as "Type paramName"
            string parameters = string.Join(", ", c.GetParameters()
                .Select(p => $"{FormatTypeName(p.ParameterType)} {p.Name}"));

            // Format the constructor signature nicely
            string constructorSignature = $"{FormatTypeName(c.DeclaringType)}({parameters})";
            return constructorSignature;
        }
    }
}
