using System;

namespace Vapor.GraphTools
{
    public abstract class PropertyNodeSo : NodeSo
    {
        public abstract Type GetValueType();
        protected abstract object GetBoxedValue();

        /// <summary>
        /// Tries to cast the boxed value of <see cref="GetBoxedValue"/> to the desired type.
        /// </summary>
        /// <typeparam name="T">The type to cast to</typeparam>
        /// <param name="value">The output value, default(T) if not successful</param>
        /// <returns>True if the cast was successful</returns>
        public bool TryGetValue<T>(out T value)
        {
            var boxed = GetBoxedValue();
            if (boxed is T val)
            {
                value = val;
                return true;
            }
            else
            {
                value = default;
                return false;
            }
        }
    }
}
