using System;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using Vapor.VisualScripting;

namespace VaporEditor.VisualScripting
{
    public static class BlueprintPortExtensions
    {
        public static PortSlot GetSlot(this Port port)
        {
            return port is BlueprintPort bpPort ? bpPort.Slot : null;
        }
    }

    public class BlueprintPort : Port
    {
        public static StyleSheet styleSheet;

        PortSlot _slot;
        public PortSlot Slot
        {
            get { return _slot; }
            set
            {
                if (ReferenceEquals(value, _slot))
                {
                    return;
                }

                if (value == null)
                {
                    throw new NullReferenceException();
                }

                if (_slot != null && value.Direction != _slot.Direction)
                {
                    throw new ArgumentException("Cannot change direction of already created port");
                }

                _slot = value;
                portName = Slot.DisplayName;
            }
        }

        public Action<Port> OnDisconnect;        

        protected BlueprintPort(Orientation portOrientation, Direction portDirection, Capacity portCapacity, Type type) : base(portOrientation, portDirection, portCapacity, type)
        {
            styleSheet = styleSheet != null ? styleSheet : Resources.Load<StyleSheet>("Styles/ShaderPort");
            styleSheets.Add(styleSheet);
        }

        public static BlueprintPort Create(PortSlot slot, IEdgeConnectorListener connectorListener)
        {
            var port = new BlueprintPort(Orientation.Horizontal, slot.Direction == PortDirection.In ? Direction.Input : Direction.Output,
                slot.AllowMultiple ? Capacity.Multi : Capacity.Single, slot.Type)
            {
                m_EdgeConnector = new EdgeConnector<Edge>(connectorListener),
            };
            port.AddManipulator(port.m_EdgeConnector);
            port.Slot = slot;
            port.portName = slot.DisplayName;
            return port;
        }

        public void Dispose()
        {
            this.RemoveManipulator(m_EdgeConnector);
            m_EdgeConnector = null;
            _slot = null;
            styleSheets.Clear();
            DisconnectAll();
            OnDisconnect = null;
        }

        public override void Disconnect(Edge edge)
        {
            base.Disconnect(edge);
            OnDisconnect?.Invoke(this);
        }
    }
}
