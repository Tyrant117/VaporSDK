using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using Vapor.GraphTools;

namespace VaporEditor.GraphTools
{
    public static class MathNodeDrawerHelper
    {
        public static bool AddNodes<GraphArg>(ScriptableObject node, GraphEditorView<GraphArg> editorView, GraphView graphView, List<IGraphToolsNode> editorNodes, List<NodeSo> refNodes) where GraphArg : ScriptableObject
        {
            if (node is MathEvaluateNodeSo resultNode)
            {
                var editorNode = NodeUtility.GetNParamNodeOrToken(editorView, graphView, resultNode);
                editorNodes.Add(editorNode);
                refNodes?.Add(resultNode);
                return true;
            }

            if (node is MathGraphNodeSo mathGraphNode)
            {
                var editorNode = new EditorMathGraphNode<GraphArg>(editorView, mathGraphNode);
                editorNode.SetPosition(mathGraphNode.Position);
                graphView.AddElement(editorNode);
                editorNodes.Add(editorNode);
                refNodes?.Add(mathGraphNode);
                return true;
            }

            if (node is MathNodeSo mathNode)
            {
                var editorNode = NodeUtility.GetNParamNodeOrToken(editorView, graphView, mathNode);
                editorNodes.Add(editorNode);
                refNodes?.Add(mathNode);
                return true;
            }
            return false;
        }
    }
}
