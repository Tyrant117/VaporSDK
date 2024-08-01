using UnityEngine;

namespace Vapor
{
    public interface IExternalValueSource
    {
        T GetExternalValue<T>(int valueKey);
    }
}
