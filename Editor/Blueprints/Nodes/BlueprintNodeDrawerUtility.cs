using System;
using System.Collections.Generic;
using Vapor.Blueprints;

namespace VaporEditor.Blueprints
{
    public static class BlueprintNodeDrawerUtility
    {
        public static bool AddNodes(BlueprintNodeDataModel node, BlueprintEditorView editorView, EdgeConnectorListener edgeConnectorListener, List<IBlueprintEditorNode> editorNodes, List<BlueprintNodeDataModel> refNodes)
        {
            if (node == null)
            {
                return false;
            }

            var editorNode = GetNodeOrToken(editorView, node, edgeConnectorListener);
            editorNodes.Add(editorNode);
            refNodes?.Add(node);
            return true;
        }
        
        public static IBlueprintEditorNode GetNodeOrToken(BlueprintEditorView editorView, BlueprintNodeDataModel node, EdgeConnectorListener edgeConnectorListener)
        {
            switch (node.NodeType)
            {
                case BlueprintNodeType.Method:
                case BlueprintNodeType.Entry:
                case BlueprintNodeType.Return:
                case BlueprintNodeType.IfElse:
                case BlueprintNodeType.ForEach:
                case BlueprintNodeType.Getter:
                case BlueprintNodeType.Setter:
                    default:
                    var editorNode = new BlueprintEditorNode(editorView, node, edgeConnectorListener);
                    editorNode.SetPosition(node.Position);
                    editorView.GraphView.AddElement(editorNode);
                    return editorNode;
                case BlueprintNodeType.Reroute:
                    var redirectNode = new BlueprintRedirectNode(editorView, node, edgeConnectorListener);
                    redirectNode.SetPosition(node.Position);
                    editorView.GraphView.AddElement(redirectNode);
                    return redirectNode;
                case BlueprintNodeType.Converter:
                    var converterNode = new BlueprintRedirectNode(editorView, node, edgeConnectorListener);
                    converterNode.SetPosition(node.Position);
                    editorView.GraphView.AddElement(converterNode);
                    return converterNode;
            }

            return null;
        }
    }
}
