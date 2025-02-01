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
            // if (node.GetType().IsDefined(typeof(BlueprintPropertyAttribute), true))
            // {
            //     var editorNode = new NParamEditorToken(editorView, node, default);
            //     editorNode.SetPosition(node.Position);
            //     editorView.GraphView.AddElement(editorNode);
            //     return editorNode;
            // }
            // else
            {
                var editorNode = new BlueprintEditorNode(editorView, node, edgeConnectorListener);
                editorNode.SetPosition(node.Position);
                editorView.GraphView.AddElement(editorNode);
                return editorNode;
            }
        }
    }
}
