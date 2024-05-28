using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using Vapor.GraphTools;

namespace VaporEditor.GraphTools
{
    public static class LogicNodeDrawerHelper
    {
        public static bool AddNodes<GraphArg>(ScriptableObject node, GraphEditorView<GraphArg> editorView, GraphView graphView, List<IGraphToolsNode> editorNodes, List<NodeSo> refNodes) where GraphArg : ScriptableObject
        {
            if (node is LogicEvaluateNodeSo resultNode)
            {
                var editorNode = new NParamEditorToken<GraphArg>(editorView, resultNode, default);
                editorNode.SetPosition(resultNode.Position);
                graphView.AddElement(editorNode);
                editorNodes.Add(editorNode);
                refNodes?.Add(resultNode);
                return true;
            }

            if (node is LogicGraphNodeSo logicGraphNode)
            {
                var editorNode = new LogicGraphEditorNode<GraphArg>(editorView, logicGraphNode);
                editorNode.SetPosition(logicGraphNode.Position);
                graphView.AddElement(editorNode);
                editorNodes.Add(editorNode);
                refNodes?.Add(logicGraphNode);
                return true;
            }

            if (node is LogicNodeSo logicNode)
            {
                var editorNode = new NParamEditorNode<GraphArg>(editorView, logicNode);
                editorNode.SetPosition(logicNode.Position);
                graphView.AddElement(editorNode);
                editorNodes.Add(editorNode);
                refNodes?.Add(logicNode);
                return true;
            }
            return false;
        }
    }
}
