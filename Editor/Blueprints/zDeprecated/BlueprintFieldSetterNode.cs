using System;
using System.Linq;
using UnityEngine.Assertions;
using Vapor.Inspector;

namespace Vapor.Blueprints
{
    // [System.Obsolete]
    // public class BlueprintFieldSetterNode : BlueprintBaseNode
    // {
    //     private readonly Delegate _function;
    //     private readonly bool _isStatic;
    //     private string _valuePinName;
    //
    //     private readonly string _nextNodeGuid;
    //     private BlueprintBaseNode _nextNode;
    //
    //     public BlueprintFieldSetterNode(BlueprintDesignNodeDto dto)
    //     {
    //         Guid = dto.Guid;
    //         dto.Properties.TryGetValue(NodePropertyNames.FIELD_TYPE, out var fieldType);
    //         dto.Properties.TryGetValue(NodePropertyNames.FIELD_NAME, out var fieldName);
    //         var fieldInfo = RuntimeReflectionUtility.GetFieldInfo((Type)fieldType.Item2, (string)fieldName.Item2); 
    //         
    //         _function = FieldDelegateHelper.GetDelegateForFieldSetter(fieldInfo);
    //         _isStatic = fieldInfo.IsStatic;
    //         // InputWires = dto.InputWires;
    //         // OutputWires = dto.OutputWires;
    //
    //         SetupInputPins(dto, p => _valuePinName = p.PinName);
    //         SetupOutputPins(dto);
    //         _nextNodeGuid = GetOutputNodeGuidForPinName(dto);
    //     }
    //
    //     public override void Init(PlayModeClass playModeClass, PlayModeMethod playModeMethod)
    //     {
    //         Method = playModeClass;
    //         if (!_nextNodeGuid.EmptyOrNull())
    //         {
    //             Method.TryGetNode(_nextNodeGuid, out _nextNode);
    //         }
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
    //             _function.DynamicInvoke(InPortValues[_valuePinName]);
    //         }
    //         else
    //         {
    //             _function.DynamicInvoke(InPortValues[PinNames.OWNER], InPortValues[_valuePinName]);
    //         }
    //     }
    //
    //     protected override void Continue()
    //     {
    //         if (!Method.IsEvaluating)
    //         {
    //             return;
    //         }
    //         
    //         _nextNode?.InvokeAndContinue();
    //     }
    // }
}