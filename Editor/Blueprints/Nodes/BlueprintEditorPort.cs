using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using Vapor.Blueprints;
using Vapor.Inspector;
using Vapor.ModelViewController;
using VaporEditor.Inspector;
using Object = UnityEngine.Object;

namespace VaporEditor.Blueprints
{
    public static class BlueprintPortExtensions
    {
        public static BlueprintPin GetPin(this Port port)
        {
            return port is BlueprintEditorPort bpPort ? bpPort.Pin : null;
        }
    }
    
    public class BlueprintEditorPort : Port, IEdgeConnectorListener
    {
        private static readonly StyleSheet s_PortColors = Resources.Load<StyleSheet>("Styles/PortColors");
        
        private static StyleSheet s_StyleSheet;

        private BlueprintPin _pin;
        public BlueprintPin Pin
        {
            get => _pin;
            private set
            {
                if (ReferenceEquals(value, _pin))
                {
                    return;
                }

                if (value == null)
                {
                    throw new NullReferenceException();
                }

                if (_pin != null && value.Direction != _pin.Direction)
                {
                    throw new ArgumentException("Cannot change direction of already created port");
                }

                _pin = value;
                portName = Pin.DisplayName;
            }
        }
        
        public SerializedPropertyType PropertyType { get; set; }
        public bool IsArray { get; set; }
        public bool IsClass { get; set; }

        public IBlueprintEditorNode Node { get; }
        public VisualElement DrawnField { get; private set; }

        public event Action<BlueprintEditorPort> Connected;
        public event Action<BlueprintEditorPort> Disconnected;

        private BlueprintEditorPort(IBlueprintEditorNode blueprintEditorNode, Orientation portOrientation, Direction portDirection, Capacity portCapacity, BlueprintPin pin) : base(portOrientation, portDirection, portCapacity, pin.Type)
        {
            Node = blueprintEditorNode;
            Pin = pin;
            s_StyleSheet = s_StyleSheet != null ? s_StyleSheet : Resources.Load<StyleSheet>("Styles/BlueprintEditorPort");
            styleSheets.Add(s_StyleSheet);
            styleSheets.Add(s_PortColors);
            RemoveFromClassList("port");
            PropertyType = TypeToSerializedPropertyType(Pin.Type);
            tooltip = Pin.CreateTooltipForPin();
            
            IsArray = IsArrayOrList(Pin.Type);
            if (IsArray)
            {
                AddToClassList("List");
            }
            if (IsDictionary(Pin.Type))
            {
                AddToClassList("Dictionary");
            }
            
            if (PropertyType == SerializedPropertyType.ManagedReference)
            {
                IsClass = Pin.Type.IsClass;
            }

            visualClass = GetVisualClass();

            DrawnField = new VisualElement();
            DrawField();
            
            this.AddManipulator(new ContextualMenuManipulator(BuildContextualMenu));
        }

        private void DrawField()
        {
            DrawnField.Clear();
            if (!Pin.HasInlineValue)
            {
                return;
            }
            
            var ve = SerializedDrawerUtility.DrawFieldFromObject(Pin.InlineValue, Pin.InlineValue.GetType());
            ve.Q<Label>()?.Hide();
            DrawnField.Add(ve);
        }

        private void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            if (evt.target is not Port)
            {
                return;
            }

            evt.menu.AppendAction("Disconnect", CTX_DisconnectAll, CTX_DisconnectAllStatus);
            evt.menu.AppendSeparator();
            evt.menu.AppendAction("Promote to Variable", CTX_PromoteToVariable, CTX_PromoteToVariableStatus);

            if (Pin.IsWildcard)
            {
                evt.menu.AppendSeparator();
                foreach (var wcType in Pin.WildcardTypes)
                {
                    evt.menu.AppendAction($"Change Type To {wcType.Name}", c => CTX_ChangeType(c, wcType), CTX_ChangeTypeStatus);
                }
            }
        }

        public static BlueprintEditorPort Create(IBlueprintEditorNode node, BlueprintPin pin)
        {
            var port = new BlueprintEditorPort(node, Orientation.Horizontal, pin.Direction == PinDirection.In ? Direction.Input : Direction.Output,
                pin.AllowMultipleWires ? Capacity.Multi : Capacity.Single, pin);
            port.m_EdgeConnector = new EdgeConnector<Edge>(port);
            port.AddManipulator(port.m_EdgeConnector);
            return port;
        }

        public override void Connect(Edge edge)
        {
            base.Connect(edge);
            AddToClassList("connected");
            if (connected)
            {
                DrawnField.Hide();
            }
            Connected?.Invoke(this);
        }

        public override void Disconnect(Edge edge)
        {
            base.Disconnect(edge);
            if (!connected)
            {
                RemoveFromClassList("connected");
                DrawnField.Show();
            }
            Disconnected?.Invoke(this);
        }

        #region - Context Menu -
        private void CTX_DisconnectAll(DropdownMenuAction obj)
        {
            Node.View.DeleteElements(connections);
        }
        
        private DropdownMenuAction.Status CTX_DisconnectAllStatus(DropdownMenuAction arg)
        {
            return connected ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled;
        }

        private void CTX_PromoteToVariable(DropdownMenuAction obj)
        {
            var baseName = $"LocalVar_{portType.Name}_";
            int suffix = 0;
            StringBuilder fieldNameBuilder = new StringBuilder(baseName);

            do
            {
                fieldNameBuilder.Length = baseName.Length; // Reset to base name length
                fieldNameBuilder.Append(suffix++);
            } 
            while (Node.View.GraphObject.DesignGraph.Current.TemporaryVariables.Exists(x => x.Name == fieldNameBuilder.ToString()));
            
            var fieldName = fieldNameBuilder.ToString();
            Node.View.GraphObject.DesignGraph.Current.TemporaryVariables.Add(new BlueprintVariable(fieldName, portType, BlueprintVariable.VariableType.Local).WithMethodGraph(Node.View.GraphObject.DesignGraph.Current));
            Node.View.Blackboard.UpdateTemporaryVariables();

            if (direction == Direction.Output)
            {
                var position = parent.LocalToWorld(layout.position) + new Vector2(106, 16);
                Node.View.OnSpawnNodeDirect<TemporaryDataSetterNodeType>(position, (INodeType.GRAPH_PARAM, Node.View.GraphObject.DesignGraph.Current), (INodeType.NAME_DATA_PARAM, fieldName));

                var last = Node.View.EditorNodes[^1];
                if (last.InPorts.TryGetValue(fieldName, out var inPort))
                {
                    Node.View.CreateEdge(this, inPort, true);
                }
            }
            else
            {
                var position = parent.LocalToWorld(layout.position) + new Vector2(-176, 16);
                Node.View.OnSpawnNodeDirect<TemporaryDataGetterNodeType>(position, (INodeType.GRAPH_PARAM, Node.View.GraphObject.DesignGraph.Current), (INodeType.NAME_DATA_PARAM, fieldName));

                var last = Node.View.EditorNodes[^1];
                if (last.OutPorts.TryGetValue(fieldName, out var outPort))
                {
                    Node.View.CreateEdge(outPort, this, true);
                }
            }

        }
        
        private DropdownMenuAction.Status CTX_PromoteToVariableStatus(DropdownMenuAction arg)
        {
            return portType == typeof(ExecutePin) ? DropdownMenuAction.Status.Hidden : DropdownMenuAction.Status.Normal;
        }

        private void CTX_ChangeType(DropdownMenuAction arg, Type newType)
        {
            Pin.Type = newType;
            UpdateType();
        }
        
        private DropdownMenuAction.Status CTX_ChangeTypeStatus(DropdownMenuAction arg)
        {
            return connected ? DropdownMenuAction.Status.Disabled : DropdownMenuAction.Status.Normal;
        }

        public void UpdateType()
        {
            portType = Pin.Type;
            PropertyType = TypeToSerializedPropertyType(Pin.Type);
            visualClass = GetVisualClass();
            tooltip = Pin.CreateTooltipForPin();
            portName = Pin.DisplayName;
            DrawField();
        }

        #endregion

        #region - Edge Connection -

        void IEdgeConnectorListener.OnDropOutsidePort(Edge edge, Vector2 position)
        {
            // var draggedPort = edge.output?.edgeConnector.edgeDragHelper.draggedPort ?? edge.input?.edgeConnector.edgeDragHelper.draggedPort;
            var screenPosition = position + Node.View.Window.position.position;
            BlueprintSearchWindow.Show(position, screenPosition, new ContextSearchProvider(Pin.Type, true, Node.View.OnSpawnNode).WithPin(this), true, false);
        }

        void IEdgeConnectorListener.OnDrop(GraphView graphView, Edge edge)
        {
            var edgesToDelete = new List<Edge>();
            if (edge.input.capacity == Capacity.Single)
            {
                foreach (Edge connection in edge.input.connections)
                {
                    if (connection != edge)
                    {
                        edgesToDelete.Add(connection);
                    }
                }
            }

            if (edge.output.capacity == Capacity.Single)
            {
                foreach (Edge connection2 in edge.output.connections)
                {
                    if (connection2 != edge)
                    {
                        edgesToDelete.Add(connection2);
                    }
                }
            }

            if (edgesToDelete.Count > 0)
            {
                graphView.DeleteElements(edgesToDelete);
            }

            if (Node.View.IsCompatiblePort(edge.output, edge.input))
            {
                if (Node.View.CanConvert(edge.output.portType, edge.input.portType))
                {
                    // Create Converter
                    Node.View.CreateConverterNode(edge);
                }
                else
                {
                    Node.View.CreateEdge((BlueprintEditorPort)edge.output, (BlueprintEditorPort)edge.input, true);
                }
            }
        }
        #endregion

        public override void DisconnectAll()
        {
            base.DisconnectAll();
            RemoveFromClassList("connected");
        }

        private string GetVisualClass()
        {
            if (portType == typeof(ExecutePin))
            {
                return "Execute";
            }

            var propType = PropertyType;
            if (PropertyType == SerializedPropertyType.Generic && portType.GetGenericArguments().Length > 0)
            {
                propType = TypeToSerializedPropertyType(portType.GetGenericArguments()[0]);
            }
            switch (propType)
            {
                case SerializedPropertyType.Generic:
                case SerializedPropertyType.Integer:
                case SerializedPropertyType.Boolean:
                case SerializedPropertyType.Float:
                case SerializedPropertyType.String:
                case SerializedPropertyType.Color:
                case SerializedPropertyType.ObjectReference:
                case SerializedPropertyType.LayerMask:
                case SerializedPropertyType.Enum:
                case SerializedPropertyType.Vector2:
                case SerializedPropertyType.Vector3:
                case SerializedPropertyType.Vector4:
                case SerializedPropertyType.Rect:
                case SerializedPropertyType.Character:
                case SerializedPropertyType.AnimationCurve:
                case SerializedPropertyType.Bounds:
                case SerializedPropertyType.Gradient:
                case SerializedPropertyType.Quaternion:
                case SerializedPropertyType.Vector2Int:
                case SerializedPropertyType.Vector3Int:
                case SerializedPropertyType.RectInt:
                case SerializedPropertyType.BoundsInt:
                case SerializedPropertyType.Hash128:
                case SerializedPropertyType.RenderingLayerMask:
                    return propType.ToString();
                case SerializedPropertyType.ManagedReference:
                    return IsClass ? "ManagedClass" : "ManagedStruct";
                default:
                    return string.Empty;
            }
        }
        
        private static SerializedPropertyType TypeToSerializedPropertyType(Type type)
        {
            switch (Type.GetTypeCode(type))
            {
                case TypeCode.SByte:
                case TypeCode.Byte:
                case TypeCode.Int16:
                case TypeCode.UInt16:
                case TypeCode.Int32:
                case TypeCode.UInt32:
                case TypeCode.Int64:
                case TypeCode.UInt64:
                    if (type.IsEnum)
                    {
                        return SerializedPropertyType.Enum;
                    }
                    return SerializedPropertyType.Integer;
                case TypeCode.Single:
                case TypeCode.Double:
                    return SerializedPropertyType.Float;
                case TypeCode.Boolean:
                    return SerializedPropertyType.Boolean;
                case TypeCode.Char:
                    return SerializedPropertyType.Character;
                case TypeCode.String:
                    return SerializedPropertyType.String;
                case TypeCode.Object:
                    if (IsArrayOrList(type) || IsDictionary(type))
                    {
                        return SerializedPropertyType.Generic;
                    }

                    if (type.IsEnum)
                    {
                        return SerializedPropertyType.Enum;
                    }

                    if (type == typeof(Color))
                    {
                        return SerializedPropertyType.Color;
                    }

                    if (type == typeof(Object) || type.IsSubclassOf(typeof(Object)))
                    {
                        return SerializedPropertyType.ObjectReference;
                    }

                    if (type == typeof(LayerMask))
                    {
                        return SerializedPropertyType.LayerMask;
                    }

                    if (type == typeof(RenderingLayerMask))
                    {
                        return SerializedPropertyType.RenderingLayerMask;
                    }

                    if (type == typeof(Vector2))
                    {
                        return SerializedPropertyType.Vector2;
                    }

                    if (type == typeof(Vector3))
                    {
                        return SerializedPropertyType.Vector3;
                    }

                    if (type == typeof(Vector4))
                    {
                        return SerializedPropertyType.Vector4;
                    }

                    if (type == typeof(Rect))
                    {
                        return SerializedPropertyType.Rect;
                    }

                    if (type == typeof(AnimationCurve) || type.IsSubclassOf(typeof(AnimationCurve)))
                    {
                        return SerializedPropertyType.AnimationCurve;
                    }

                    if (type == typeof(Bounds))
                    {
                        return SerializedPropertyType.Bounds;
                    }

                    if (type == typeof(Gradient) || type.IsSubclassOf(typeof(Gradient)))
                    {
                        return SerializedPropertyType.Gradient;
                    }

                    if (type == typeof(Quaternion))
                    {
                        return SerializedPropertyType.Quaternion;
                    }

                    if (type == typeof(Vector2Int))
                    {
                        return SerializedPropertyType.Vector2Int;
                    }

                    if (type == typeof(Vector3Int))
                    {
                        return SerializedPropertyType.Vector3Int;
                    }

                    if (type == typeof(RectInt))
                    {
                        return SerializedPropertyType.RectInt;
                    }

                    if (type == typeof(BoundsInt))
                    {
                        return SerializedPropertyType.BoundsInt;
                    }

                    if (type == typeof(Hash128))
                    {
                        return SerializedPropertyType.Hash128;
                    }

                    return SerializedPropertyType.ManagedReference;
                default:
                    return SerializedPropertyType.Generic;
            }
        }
        
        private static bool IsArrayOrList(Type type)
        {
            // Check if the type is an array
            if (type.IsArray)
            {
                return true;
            }
            

            // Check if the type is a List<> or a derived type
            return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>);
        }

        private static bool IsDictionary(Type type)
        {
            return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Dictionary<,>);
        }
    }
}
