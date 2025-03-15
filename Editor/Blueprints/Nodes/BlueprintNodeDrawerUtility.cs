using System;
using System.Collections.Generic;
using Vapor.Blueprints;

namespace VaporEditor.Blueprints
{
    public static class BlueprintNodeDrawerUtility
    {
        private static readonly Dictionary<Type, Func<BlueprintView, BlueprintDesignNode, IBlueprintEditorNode>> s_NodeFactory = new()
        {
            { typeof(EntryNodeType), CreateEditorNode },
            { typeof(ReturnNodeType), CreateEditorNode },
            { typeof(GraphNodeType), CreateEditorNode },
            
            { typeof(MethodNodeType), CreateEditorNode },
            
            { typeof(BranchNodeType), CreateEditorNode },
            { typeof(SwitchNodeType), CreateEditorNode },
            { typeof(WhileNodeType), CreateEditorNode },
            { typeof(SequenceNodeType), CreateEditorNode },
            { typeof(ForEachNodeType), CreateEditorNode },
            { typeof(ForNodeType), CreateEditorNode },
            
            { typeof(TemporaryDataGetterNodeType), CreateEditorNode },
            { typeof(TemporaryDataSetterNodeType), CreateEditorNode },
            { typeof(FieldGetterNodeType), CreateEditorNode },
            { typeof(FieldSetterNodeType), CreateEditorNode },
            { typeof(MakeSerializableNodeType), CreateEditorNode },
            
            { typeof(RerouteNodeType), CreateRerouteNode },
            { typeof(ConverterNodeType), CreateRerouteNode },
        };
        
        public static void AddNode(BlueprintDesignNode node, BlueprintView view, List<IBlueprintEditorNode> editorNodes, List<BlueprintDesignNode> refNodes)
        {
            if (node == null || !s_NodeFactory.TryGetValue(node.Type, out var func))
            {
                return;
            }

            var editorNode = func(view, node);
            editorNodes.Add(editorNode);
            refNodes?.Add(node);
        }

        private static IBlueprintEditorNode CreateEditorNode(BlueprintView view, BlueprintDesignNode node)
        {
            var editorNode = new BlueprintEditorNode(view, node);
            editorNode.SetPosition(node.Position);
            view.AddElement(editorNode);
            return editorNode;
        }
        
        private static IBlueprintEditorNode CreateRerouteNode(BlueprintView view, BlueprintDesignNode node)
        {
            var redirectNode = new BlueprintRedirectNode(view, node);
            redirectNode.SetPosition(node.Position);
            view.AddElement(redirectNode);
            return redirectNode;
        }
    }
}
