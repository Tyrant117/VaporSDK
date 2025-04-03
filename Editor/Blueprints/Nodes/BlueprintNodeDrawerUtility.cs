using System;
using System.Collections.Generic;
using Vapor.Blueprints;

namespace VaporEditor.Blueprints
{
    public static class BlueprintNodeDrawerUtility
    {
        private static readonly Dictionary<NodeType, Func<BlueprintView, BlueprintNodeController, IBlueprintNodeView>> s_NodeFactory = new()
        {
            { NodeType.Entry, CreateEditorNode },
            { NodeType.Method, CreateEditorNode },
            { NodeType.MemberAccess, CreateEditorNode },
            { NodeType.Return, CreateEditorNode },
            
            { NodeType.Branch, CreateEditorNode },
            { NodeType.Switch, CreateEditorNode },
            { NodeType.Sequence, CreateEditorNode<BlueprintSequenceNodeView> },
            
            { NodeType.For, CreateEditorNode },
            { NodeType.ForEach, CreateEditorNode },
            { NodeType.While, CreateEditorNode },
            { NodeType.Break, CreateEditorNode },
            { NodeType.Continue, CreateEditorNode },
            
            { NodeType.Conversion, CreateRerouteNode },
            { NodeType.Cast, CreateEditorNode<BlueprintCastNodeView> },
            
            { NodeType.Redirect, CreateRerouteNode },
            { NodeType.Inline, CreateEditorNode },
        };
        
        public static void AddNode(BlueprintNodeController nodeController, BlueprintView view, List<IBlueprintNodeView> editorNodes, List<BlueprintNodeController> refNodes)
        {
            if (nodeController == null || !s_NodeFactory.TryGetValue(nodeController.Model.NodeType, out var func))
            {
                return;
            }

            var editorNode = func(view, nodeController);
            editorNodes.Add(editorNode);
            refNodes?.Add(nodeController);
        }

        private static IBlueprintNodeView CreateEditorNode(BlueprintView view, BlueprintNodeController nodeController)
        {
            var editorNode = new BlueprintNodeView(view, nodeController);
            editorNode.SetPosition(nodeController.Model.Position);
            view.AddElement(editorNode);
            return editorNode;
        }
        
        private static IBlueprintNodeView CreateEditorNode<T>(BlueprintView view, BlueprintNodeController nodeController) where T : BlueprintNodeView
        {
            var editorNode = Activator.CreateInstance(typeof(T), view, nodeController) as T;
            editorNode.SetPosition(nodeController.Model.Position);
            view.AddElement(editorNode);
            return editorNode;
        }
        
        private static IBlueprintNodeView CreateRerouteNode(BlueprintView view, BlueprintNodeController nodeController)
        {
            var redirectNode = new BlueprintRedirectNodeView(view, nodeController);
            redirectNode.SetPosition(nodeController.Model.Position);
            view.AddElement(redirectNode);
            return redirectNode;
        }
    }
}
