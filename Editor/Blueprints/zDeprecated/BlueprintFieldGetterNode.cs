using System;
using System.Collections.Generic;

namespace Vapor.Blueprints
{
    // [System.Obsolete]
    // public class BlueprintFieldGetterNode : BlueprintBaseNode
    // {
    //     private readonly Delegate _function;
    //     private readonly bool _isStatic;
    //
    //     public BlueprintFieldGetterNode(BlueprintDesignNodeDto dto)
    //     {
    //         Guid = dto.Guid;
    //         dto.Properties.TryGetValue(NodePropertyNames.FIELD_TYPE, out var fieldType);
    //         dto.Properties.TryGetValue(NodePropertyNames.FIELD_NAME, out var fieldName);
    //         var fieldInfo = RuntimeReflectionUtility.GetFieldInfo((Type)fieldType.Item2, (string)fieldName.Item2);
    //         _function = FieldDelegateHelper.GetDelegateForFieldGetter(fieldInfo);
    //         _isStatic = fieldInfo.IsStatic;
    //         
    //         InPortValues = new Dictionary<string, object>(dto.InputPins.Count)
    //         {
    //             [PinNames.OWNER] = null
    //         };
    //         OutPortValues = new Dictionary<string, object>(dto.OutputPins.Count)
    //         {
    //             [PinNames.RETURN] = null
    //         };
    //     }
    //
    //     public override void Init(PlayModeClass playModeClass, PlayModeMethod playModeMethod)
    //     {
    //         Method = playModeClass;
    //     }
    //     
    //     protected override void CacheInputValues()
    //     {
    //         GetAllInputPinValues();
    //     }
    //
    //     protected override void WriteOutputValues()
    //     {
    //         if (_function == null)
    //         {
    //             return;
    //         }
    //
    //         if (_isStatic)
    //         {
    //             OutPortValues[PinNames.RETURN] = _function.DynamicInvoke();
    //         }
    //         else
    //         {
    //             OutPortValues[PinNames.RETURN] = _function.DynamicInvoke(InPortValues[PinNames.OWNER]);
    //         }
    //     }
    //
    //     protected override void Continue()
    //     {
    //     }
    // }
}