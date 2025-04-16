using System.Collections.Generic;
using UnityEngine;
using Vapor.Blueprints;
using GraphElement = UnityEditor.Experimental.GraphView.GraphElement;

namespace VaporEditor.Blueprints
{
    public static class BlueprintClipboardUtility
    {
        private static object s_Buffer;
        public static bool CanPaste => s_Buffer != null;

        public static void Cut(GraphElement graphElement)
        {
            switch (graphElement)
            {
                case null:
                    return;
                case BlueprintNodeView nodeView:
                {
                    s_Buffer = nodeView.Controller.Serialize();
                    nodeView.Delete();
                    break;
                }
                case BlueprintRedirectNodeView redirectNodeView:
                {
                    s_Buffer = redirectNodeView.Controller.Serialize();
                    redirectNodeView.Delete();
                    break;
                }
            }
        }
        
        public static void Cut(IEnumerable<GraphElement> graphElements)
        {
            List<BlueprintDesignNodeDto> copy = new();
            foreach (var node in graphElements)
            {
                switch (node)
                {
                    case BlueprintNodeView nodeView:
                    {
                        copy.Add(nodeView.Controller.Serialize());
                        nodeView.Delete();
                        break;
                    }
                    case BlueprintRedirectNodeView redirectNodeView:
                    {
                        copy.Add(redirectNodeView.Controller.Serialize());
                        redirectNodeView.Delete();
                        break;
                    }
                }
            }
            
            s_Buffer = copy.Count > 0 ? copy : null;
        }
        
        public static void Copy(GraphElement graphElement)
        {
            switch (graphElement)
            {
                case null:
                    return;
                case BlueprintNodeView nodeView:
                {
                    s_Buffer = nodeView.Controller.Serialize();
                    break;
                }
                case BlueprintRedirectNodeView redirectNodeView:
                {
                    s_Buffer = redirectNodeView.Controller.Serialize();
                    break;
                }
            }
        }

        public static void Copy(IEnumerable<GraphElement> graphElements)
        {
            List<BlueprintDesignNodeDto> copy = new();
            foreach (var node in graphElements)
            {
                switch (node)
                {
                    case BlueprintNodeView nodeView:
                    {
                        copy.Add(nodeView.Controller.Serialize());
                        break;
                    }
                    case BlueprintRedirectNodeView redirectNodeView:
                    {
                        copy.Add(redirectNodeView.Controller.Serialize());
                        break;
                    }
                }
            }
            
            s_Buffer = copy.Count > 0 ? copy : null;
        }
        
        public static void Paste(BlueprintView blueprintView, Vector2 position)
        {
            switch (s_Buffer)
            {
                case null:
                    return;
                case BlueprintDesignNodeDto nodeModelBase:
                {
                    var pastedNode = NodeFactory.Build(nodeModelBase, blueprintView.Method);
                    pastedNode.InputPins.Clear();
                    pastedNode.OutputPins.Clear();
                    pastedNode.BuildPins();
                    pastedNode.PostBuildData();
                    pastedNode.Position = new Rect(position.x, position.y, 0, 0);
                    blueprintView.Method.PasteNode(pastedNode);
                    break;
                }
                case List<BlueprintDesignNodeDto> nodeModelList:
                {
                    foreach (var nodeModel in nodeModelList)
                    {
                        var pastedNode = NodeFactory.Build(nodeModel, blueprintView.Method);
                        pastedNode.InputPins.Clear();
                        pastedNode.OutputPins.Clear();
                        pastedNode.BuildPins();
                        pastedNode.PostBuildData();
                        pastedNode.Position = new Rect(position.x, position.y, 0, 0);
                        blueprintView.Method.PasteNode(pastedNode);
                    }

                    break;
                }
            }
        }

        public static void Duplicate(GraphElement graphElement, BlueprintView blueprintView, Vector2 position)
        {
            Copy(graphElement);
            Paste(blueprintView, position);
        }

        public static void Duplicate(IEnumerable<GraphElement> graphElements, BlueprintView blueprintView, Vector2 position)
        {
            Copy(graphElements);
            Paste(blueprintView, position);
        }
    }
}
