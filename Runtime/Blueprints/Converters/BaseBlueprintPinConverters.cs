using System;
using UnityEngine;

namespace Vapor.Blueprints
{
    public static class BaseBlueprintPinConverters
    {
        [BlueprintPinConverter(typeof(int), typeof(double))]
        public static object IntToDouble (object from)
        {
            if (from is FieldWrapper wrapper)
            {
                return Convert.ToDouble(wrapper.Get());
            }
            return Convert.ToDouble(from);
        }
    }
}
