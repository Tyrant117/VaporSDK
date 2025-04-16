using System;
using System.Collections.Generic;
using UnityEngine;
using Vapor.Blueprints;

namespace VaporEditor.Blueprints
{
    public static class BlueprintNodeDrawerUtility
    {
        private static readonly Dictionary<NodeType, Func<BlueprintView, NodeModelBase, IBlueprintNodeView>> s_NodeFactory = new()
        {
            { NodeType.Entry, CreateEditorNode },
            { NodeType.Method, CreateEditorNode },
            { NodeType.MemberAccess, CreateEditorNode },
            { NodeType.Return, CreateEditorNode },
            
            { NodeType.Branch, CreateEditorNode },
            { NodeType.Switch, CreateEditorNode<BlueprintSwitchNodeView> },
            { NodeType.Sequence, CreateEditorNode<BlueprintSequenceNodeView> },
            
            { NodeType.For, CreateEditorNode },
            { NodeType.ForEach, CreateEditorNode },
            { NodeType.While, CreateEditorNode },
            { NodeType.Break, CreateEditorNode },
            { NodeType.Continue, CreateEditorNode },
            
            { NodeType.Conversion, CreateRerouteNode },
            { NodeType.Cast, CreateEditorNode<BlueprintCastNodeView> },
            
            { NodeType.Redirect, CreateRerouteNode },
            { NodeType.Constructor, CreateEditorNode<BlueprintConstructorNodeView> },
        };
        
        public static void AddNode(NodeModelBase nodeController, BlueprintView view, List<IBlueprintNodeView> editorNodes, Dictionary<string, NodeModelBase> refNodes)
        {
            if (nodeController == null || !s_NodeFactory.TryGetValue(nodeController.NodeType, out var func))
            {
                return;
            }

            var editorNode = func(view, nodeController);
            editorNodes.Add(editorNode);
            refNodes?.Add(nodeController.Guid, nodeController);
        }

        public static IBlueprintNodeView CreateNodeView(BlueprintView view, NodeModelBase node)
        {
            if (!s_NodeFactory.TryGetValue(node.NodeType, out var func))
            {
                Debug.LogError($"No factory for {node.NodeType}");
                return null;
            }
            var editorNode = func(view, node);
            return editorNode;
        }

        private static IBlueprintNodeView CreateEditorNode(BlueprintView view, NodeModelBase nodeController)
        {
            var editorNode = new BlueprintNodeView(view, nodeController);
            editorNode.SetPosition(nodeController.Position);
            view.AddElement(editorNode);
            return editorNode;
        }
        
        private static IBlueprintNodeView CreateEditorNode<T>(BlueprintView view, NodeModelBase nodeController) where T : BlueprintNodeView
        {
            var editorNode = Activator.CreateInstance(typeof(T), view, nodeController) as T;
            editorNode.SetPosition(nodeController.Position);
            view.AddElement(editorNode);
            return editorNode;
        }
        
        private static IBlueprintNodeView CreateRerouteNode(BlueprintView view, NodeModelBase nodeController)
        {
            var redirectNode = new BlueprintRedirectNodeView(view, nodeController);
            redirectNode.SetPosition(nodeController.Position);
            view.AddElement(redirectNode);
            return redirectNode;
        }
    }
}
