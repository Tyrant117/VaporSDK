using System.Collections;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using Vapor.GraphTools;

namespace VaporEditor.GraphTools
{
    public static class NParamNodeDrawerHelper
    {
        public static bool AddNodes<GraphArg>(ScriptableObject node, GraphEditorView<GraphArg> editorView, GraphView graphView, List<IGraphToolsNode> editorNodes, List<NodeSo> refNodes) where GraphArg : ScriptableObject
        {
            if (node is NodeSo nodeSo)
            {
                var editorNode = NodeUtility.GetNParamNodeOrToken(editorView, graphView, nodeSo);
                editorNodes.Add(editorNode);
                refNodes?.Add(nodeSo);
                return true;
            }
            return false;
        }
    }
}
