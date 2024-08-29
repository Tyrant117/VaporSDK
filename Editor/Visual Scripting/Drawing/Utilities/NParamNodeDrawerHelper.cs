using System.Collections.Generic;
using UnityEngine;
using Vapor.VisualScripting;

namespace VaporEditor.VisualScripting
{
    public static class NParamNodeDrawerHelper
    {
        public static bool AddNodes(NodeModel node, GraphEditorView editorView, EdgeConnectorListener edgeConnectorListener, List<IGraphEditorNode> editorNodes, List<NodeModel> refNodes)
        {
            if (node != null)
            {
                var editorNode = GetNParamNodeOrToken(editorView, node, edgeConnectorListener);
                editorNodes.Add(editorNode);
                refNodes?.Add(node);
                return true;
            }
            return false;
        }

        public static IGraphEditorNode GetNParamNodeOrToken(GraphEditorView editorView, NodeModel node, EdgeConnectorListener edgeConnectorListener)
        {
            if (node.GetType().IsDefined(typeof(NodeIsTokenAttribute), true))
            {
                var editorNode = new NParamEditorToken(editorView, node, default);
                editorNode.SetPosition(node.Position);
                editorView.GraphView.AddElement(editorNode);
                return editorNode;
            }
            else
            {
                var editorNode = new NParamEditorNode(editorView, node, edgeConnectorListener);
                editorNode.SetPosition(node.Position);
                editorView.GraphView.AddElement(editorNode);
                return editorNode;
            }
        }
    }
}
