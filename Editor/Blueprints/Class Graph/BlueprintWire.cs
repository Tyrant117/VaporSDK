using System;
using Vapor.Inspector;
using VaporEditor.Blueprints;

namespace Vapor.Blueprints
{
    public class BlueprintWire : IBlueprintGraphModel
    {
        public enum  ChangeType
        {
            Connected,
            Disconnected,
            Deleted,
        }
        
        public string Guid { get; set; }
        public bool IsExecuteWire { get; set; }

        public string LeftGuid { get; set; }
        public string LeftName { get; set; }
        
        public string RightGuid { get; set; }
        public string RightName { get; set; }

        public event Action<BlueprintWire, ChangeType> Changed;


        public BlueprintMethodGraph Method { get; }

        public BlueprintWire(BlueprintMethodGraph method)
        {
            Method = method;
            Guid = System.Guid.NewGuid().ToString();
        }

        public BlueprintWire(BlueprintMethodGraph method, BlueprintWireDto dto)
        {
            Method = method;
            Guid = dto.Guid;
            IsExecuteWire = dto.IsExecuteWire;
            
            LeftGuid = dto.LeftGuid;
            LeftName = dto.LeftName;
            
            RightGuid = dto.RightGuid;
            RightName = dto.RightName;
        }

        public bool IsConnected()
        {
            if (LeftGuid.EmptyOrNull() || RightGuid.EmptyOrNull())
            {
                return false;
            }

            if (!Method.Nodes.TryGetValue(LeftGuid, out var leftNode))
            {
                return false;
            }
            
            if (!Method.Nodes.TryGetValue(RightGuid, out var rightNode))
            {
                return false;
            }
            
            return leftNode.OutputPins.ContainsKey(LeftName) && rightNode.InputPins.ContainsKey(RightName);
        }

        public void Connect(BlueprintPin leftPin, BlueprintPin rightPin)
        {
            IsExecuteWire = leftPin.IsExecutePin;
            
            LeftGuid = leftPin.Node.Guid;
            LeftName = leftPin.PortName;
            leftPin.Connect(this);
            
            RightGuid = rightPin.Node.Guid;
            RightName = rightPin.PortName;
            rightPin.Connect(this);
            
            Changed?.Invoke(this, ChangeType.Connected);
            Method.OnWireUpdated(this);
        }
        
        public void Disconnect()
        {
            LeftGuid = null;
            LeftName = null;
            
            RightGuid = null;
            RightName = null;
            
            Changed?.Invoke(this, ChangeType.Disconnected);
            Method.RemoveWire(this);
        }

        public void Delete()
        {
            Changed?.Invoke(this, ChangeType.Deleted);
            Method.RemoveWire(this);
        }

        public BlueprintWireDto Serialize()
        {
            return new BlueprintWireDto()
            {
                Guid = Guid,
                IsExecuteWire = IsExecuteWire,
                
                LeftGuid = LeftGuid,
                LeftName = LeftName,
                
                RightGuid = RightGuid,
                RightName = RightName,
            };
        }
    }
}