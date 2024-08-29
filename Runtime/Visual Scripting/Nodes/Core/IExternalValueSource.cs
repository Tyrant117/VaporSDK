using UnityEngine;

namespace Vapor.VisualScripting
{
    public interface IExternalValueSource
    {
        T GetExternalValue<T>(int valueKey);
    }
}
