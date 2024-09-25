using UnityEngine;

namespace Vapor
{
    public static class GameObjectExtensions
    {
        public static T OrNull<T>(this T @object) where T : Object
        {
            return @object ? @object : null;
        }

        public static T OrObjectNull<T>(this T nullable) where T : class
        {
            Object @object = nullable as Object;
            return @object ? nullable : null;
        }

        /// <summary>
        /// This method is used when an interface is expected to be implemented by an Object.
        /// It first trys to cast to the Object then performs the default unity null check.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="nullable"></param>
        /// <returns></returns>
        public static bool IsObjectNull<T>(this T nullable) where T : class
        {
            Object @object = nullable as Object;
            return @object ? false : true;
        }
    }
}