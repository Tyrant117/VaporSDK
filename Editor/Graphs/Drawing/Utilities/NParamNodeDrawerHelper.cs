using System.Collections.Generic;
using UnityEngine;
using Vapor.Graphs;

namespace VaporEditor.Graphs
{
    public static class NParamNodeDrawerHelper
    {
        public static bool AddNodes(NodeModel node, GraphEditorView editorView, List<IGraphEditorNode> editorNodes, List<NodeModel> refNodes)
        {
            if (node != null)
            {
                var editorNode = GetNParamNodeOrToken(editorView, node);
                editorNodes.Add(editorNode);
                refNodes?.Add(node);
                return true;
            }
            return false;
        }

        public static IGraphEditorNode GetNParamNodeOrToken(GraphEditorView editorView, NodeModel node)
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
                var editorNode = new NParamEditorNode(editorView, node);
                editorNode.SetPosition(node.Position);
                editorView.GraphView.AddElement(editorNode);
                return editorNode;
            }
        }
    }
}
