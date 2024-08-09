using UnityEngine;

namespace Vapor.Graphs
{
    public interface IExternalValueSource
    {
        T GetExternalValue<T>(int valueKey);
    }
}
