using System;
using System.Linq;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using Vapor.Blueprints;
using VaporEditor.Blueprints;

namespace VaporEditor.Blueprints
{
    public class BlueprintWireView : Edge
    {
        private BlueprintView _view;
        private BlueprintWire _wire;

        public BlueprintWireView()
        {
            
        }

        public void Init(BlueprintView view, BlueprintWire wire)
        {
            _view = view;
            _wire = wire;
            RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
            RegisterCallback<MouseDownEvent>(OnEdgeMouseDown);
        }

        private void OnAttachToPanel(AttachToPanelEvent evt)
        {
            _wire.Changed += OnWireChanged;
        }

        private void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            _wire.Changed -= OnWireChanged;
        }
        
        private void OnWireChanged(BlueprintWire wire, BlueprintWire.ChangeType changeType)
        {
            switch (changeType)
            {
                case BlueprintWire.ChangeType.Connected:
                {
                    if (_view.Nodes.TryGetValue(wire.LeftGuid, out var leftNode) && leftNode.OutPorts.TryGetValue(wire.LeftName, out var leftPort))
                    {
                        leftPort.Connect(this);
                    }

                    if (_view.Nodes.TryGetValue(wire.RightGuid, out var rightNode) && rightNode.InPorts.TryGetValue(wire.RightName, out var rightPort))
                    {
                        rightPort.Connect(this);
                    }

                    break;
                }
                case BlueprintWire.ChangeType.Disconnected:
                {
                    input?.Disconnect(this);
                    output?.Disconnect(this);
                    break;
                }
                case BlueprintWire.ChangeType.Deleted:
                {
                    Debug.Log("Deleted Wire");
                    input?.Disconnect(this);
                    output?.Disconnect(this);
                    break;
                }
                default:
                    throw new ArgumentOutOfRangeException(nameof(changeType), changeType, null);
            }
        }
        
        private void OnEdgeMouseDown(MouseDownEvent evt)
        {
            // Only Double Click
            if (evt.button != (int)MouseButton.LeftMouse || evt.clickCount != 2)
            {
                return;
            }

            // Only Edges
            if (evt.target != this)
            {
                return;
            }

            Vector2 pos = evt.mousePosition;
            _view.CreateRedirectNode(pos, this);
        }

        public void Delete()
        {
            _wire.Delete();
        }
    }
}
