using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using Vapor.GraphTools;

namespace VaporEditor.GraphTools
{
    public static class ValueNodeDrawerHelper
    {
        public static bool AddNodes<GraphArg>(ScriptableObject node, GraphEditorView<GraphArg> editorView, GraphView graphView, List<IGraphToolsNode> editorNodes, List<NodeSo> refNodes) where GraphArg : ScriptableObject
        {
            switch (node)
            {
                case FloatValueNodeSo floatNode:
                    {
                        var editorNode = new PropertyEditorToken<GraphArg>(editorView, floatNode, default);
                        editorNode.SetPosition(floatNode.Position);
                        graphView.AddElement(editorNode);
                        editorNodes.Add(editorNode);
                        refNodes?.Add(floatNode);
                        return true;
                    }

                case IntValueNodeSo intNode:
                    {
                        var editorNode = new PropertyEditorToken<GraphArg>(editorView, intNode, default);
                        editorNode.SetPosition(intNode.Position);
                        graphView.AddElement(editorNode);
                        editorNodes.Add(editorNode);
                        refNodes?.Add(intNode);
                        return true;
                    }

                case BoolValueNodeSo boolNode:
                    {
                        var editorNode = new PropertyEditorToken<GraphArg>(editorView, boolNode, default);
                        editorNode.SetPosition(boolNode.Position);
                        graphView.AddElement(editorNode);
                        editorNodes.Add(editorNode);
                        refNodes?.Add(boolNode);
                        return true;
                    }

                case ExposedPropertyNodeSo propertyNode:
                    {
                        var editorNode = new ExposedPropertyEditorToken<GraphArg>(editorView, propertyNode, default);
                        editorNode.SetPosition(propertyNode.Position);
                        graphView.AddElement(editorNode);
                        editorNodes.Add(editorNode);
                        refNodes?.Add(propertyNode);
                        return true;
                    }

                case TimeValueNodeSo timeNode:
                    {
                        var editorNode = NodeUtility.GetNParamNodeOrToken(editorView, graphView, timeNode);
                        editorNodes.Add(editorNode);
                        refNodes?.Add(timeNode);
                        return true;
                    }

                case DeltaTimeValueNodeSo deltaTimeNode:
                    {
                        var editorNode = NodeUtility.GetNParamNodeOrToken(editorView, graphView, deltaTimeNode);
                        editorNodes.Add(editorNode);
                        refNodes?.Add(deltaTimeNode);
                        return true;
                    }

                case FixedDeltaTimeValueNodeSo fixedDeltaTimeNode:
                    {
                        var editorNode = NodeUtility.GetNParamNodeOrToken(editorView, graphView, fixedDeltaTimeNode);
                        editorNodes.Add(editorNode);
                        refNodes?.Add(fixedDeltaTimeNode);
                        return true;
                    }
            }
            return false;
        }
    }
}
