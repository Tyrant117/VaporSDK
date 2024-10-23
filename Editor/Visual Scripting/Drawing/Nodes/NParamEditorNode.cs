using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using Vapor.VisualScripting;
using Vapor.Inspector;
using VaporEditor.Inspector;
using NodeModel = Vapor.VisualScripting.NodeModel;

namespace VaporEditor.VisualScripting
{
    public class NParamEditorNode : GraphToolsNode<NodeModel>, IInspectableNode
    {
        private static readonly StyleSheet s_PortColors = Resources.Load<StyleSheet>("Styles/PortColors");

        private List<FieldInfo> _nodeContentData;

        public SelectableManipulator Selectable { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public NParamEditorNode(BlueprintGraphEditorView view, NodeModel node, EdgeConnectorListener edgeConnectorListener)
        {
            View = view;
            Node = node;
            Node.RenameNode = OnRenameNode;
            FindParams();

            m_CollapseButton.RemoveFromHierarchy();
            StyleNode();
            var nameTuple = node.GetNodeName();
            var iconTuple = node.GetNodeNameIcon();
            CreateTitle(nameTuple.Item1, nameTuple.Item2, iconTuple.Item1, iconTuple.Item2);

            CreateAdditionalContent(mainContainer.Q("contents"));

            CreateFlowInPorts(edgeConnectorListener);
            CreateFlowOutPorts(edgeConnectorListener);

            RefreshExpandedState();
        }

        private void FindParams()
        {
            // Get all fields of the class
            var fields = Node.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy);


            // Loop through each field and check if it has the MathNodeParamAttribute
            _nodeContentData = new();
            foreach (FieldInfo field in fields)
            {
                if (field.IsDefined(typeof(NodeContentAttribute)))
                {
                    _nodeContentData.Add(field);
                }
            }
        }

        private void StyleNode()
        {
            var nodeType = Node.GetType();
            var width = nodeType.GetCustomAttribute<NodeWidthAttribute>();
            if (width != null)
                style.minWidth = width.MinWidth;
        }

        private void CreateTitle(string title, Color titleTint, Sprite titleIcon, Color titleIconTint)
        {
            this.title = title;
            var label = titleContainer.Q<Label>();
            label.style.color = titleTint;
            label.style.flexGrow = 1;
            label.style.unityTextAlign = TextAnchor.MiddleCenter;
            label.style.marginRight = 6;
            label.style.marginLeft = 6;

            if (titleIcon != null)
            {
                titleContainer.Insert(0, new Image()
                {
                    sprite = titleIcon,
                    tintColor = titleIconTint,
                    scaleMode = ScaleMode.ScaleToFit,
                    style =
                    {
                        maxWidth = 16,
                        maxHeight = 16,
                        alignSelf = Align.Center,
                        marginLeft = 6,
                    }
                });
            }
        }

        private void OnRenameNode()
        {
            Debug.Log($"Setting Title: [{GetNode().GetNodeName()}]");
            var nameTuple = GetNode().GetNodeName();
            title = nameTuple.Item1;
            titleContainer.Q<Label>().style.color = nameTuple.Item2;
            var iconTuple = GetNode().GetNodeNameIcon();
            if (iconTuple.Item1 != null)
            {
                var image = titleContainer.Q<Image>();
                if(image == null)
                {
                    titleContainer.Insert(0,new Image()
                    {
                        sprite = iconTuple.Item1,
                        tintColor = iconTuple.Item2,
                        scaleMode = ScaleMode.ScaleToFit,
                        style =
                    {
                        maxWidth = 16,
                        maxHeight = 16,
                        alignSelf = Align.Center,
                        marginLeft = 6,
                    }
                    });
                }
                else
                {
                    image.sprite = iconTuple.Item1;
                    image.tintColor = iconTuple.Item2;
                }
            }
        }

        public override void RedrawPorts(EdgeConnectorListener edgeConnectorListener)
        {
            inputContainer.DisconnectChildren();
            outputContainer.DisconnectChildren();

            CreateFlowInPorts(edgeConnectorListener);
            CreateFlowOutPorts(edgeConnectorListener);
        }

        private void CreateFlowInPorts(EdgeConnectorListener edgeConnectorListener)
        {
            InPorts = new(Node.InSlots.Count);
            foreach (var slot in Node.InSlots.Values)
            {
                var port = BlueprintPort.Create(slot, edgeConnectorListener);
                if (slot.IsOptional)
                {
                    port.AddToClassList("optionalPort");
                }
                port.tooltip = "The flow input";
                port.styleSheets.Add(s_PortColors);
                InPorts.Add(slot.UniqueName, port);
                inputContainer.Add(port);

                if (slot.HasContent)
                {
                    var ve = DrawerUtility.DrawVaporFieldFromType(slot, slot.ContentType, slot.GetContentFieldInfo(), true);
                    inputContainer.Add(ve);
                    ConnectedPort += (p) =>
                    {
                        if (p != slot.UniqueName)
                        {
                            return;
                        }
                        ve.Hide();
                    };
                    DisconnectedPort += (p) =>
                    {
                        if (p != slot.UniqueName)
                        {
                            return;
                        }
                        ve.Show();
                    };
                }
            }
        }

        private void CreateFlowOutPorts(EdgeConnectorListener edgeConnectorListener)
        {
            OutPorts = new(Node.OutSlots.Count);
            foreach (var slot in Node.OutSlots.Values)
            {
                var port = BlueprintPort.Create(slot, edgeConnectorListener);
                if (slot.IsOptional)
                {
                    port.AddToClassList("optionalPort");
                }
                port.tooltip = "The flow output";
                port.styleSheets.Add(s_PortColors);
                OutPorts.Add(slot.UniqueName, port);
                outputContainer.Add(port);
            }
        }

        protected virtual void CreateAdditionalContent(VisualElement content)
        {
            if (_nodeContentData.Count > 0)
            {
                var foldout = new StyledFoldout("Content");
                foreach (var field in _nodeContentData)
                {
                    var ve = DrawerUtility.DrawVaporFieldFromType(Node, field, true);
                    foldout.Add(ve);
                }
                content.Add(foldout);
            }
        }

        public override void OnSelected()
        {
            base.OnSelected();
            View.ViewController.View.Update();
        }

        public override void OnUnselected()
        {
            base.OnUnselected();
            View?.ViewController?.View?.Update();
        }

        public VisualElement DrawElement()
        {
            var obj = GetNode().GraphSettingsInspector();
            if (obj == null)
            {
                return null;
            }

            InspectorTreeObject ito = new(obj, obj.GetType());
            InspectorTreeRootElement root = new(ito);

            var ve = new VisualElement();
            root.DrawToScreen(ve);
            return ve;
        }
    }
}
