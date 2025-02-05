using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using Vapor.Blueprints;
using VaporEditor.Inspector;
using ISelectable = UnityEditor.Experimental.GraphView.ISelectable;

namespace VaporEditor.Blueprints
{
    public class BlueprintGraphView : GraphView
    {
        public BlueprintEditorView View { get; }
        public BlueprintGraphSo GraphObject { get; }

        public List<ISelectable> GetSelection => selection;

        public delegate void SelectionChanged(List<ISelectable> selection);
        public SelectionChanged OnSelectionChange;

        public BlueprintGraphView(BlueprintEditorView view, BlueprintGraphSo graphObject)
        {
            View = view;
            GraphObject = graphObject;
        }

        #region - Nodes -
        public void CreateRedirectNode(Vector2 pos, Edge edgeTarget)
        {
            var outputPort = (BlueprintEditorPort)edgeTarget.output;
            var inputPort = (BlueprintEditorPort)edgeTarget.input;
            
            var outputSlot = edgeTarget.output.GetPin();
            var inputSlot = edgeTarget.input.GetPin();
            
            var sr = new SearcherItem("");
            var bse = new BlueprintSearchEntry.Builder().WithFullName("Reroute").WithNodeType(BlueprintNodeType.Reroute).WithTypes(inputSlot.Type).Build();
            sr.UserData = (bse, pos);
            
            View.Select(sr);

            var reroute = View.EditorNodes[^1];
            Debug.Log(reroute.Node.NodeName);
            var leftPortRef = new BlueprintPinReference(outputSlot.PortName, outputPort.Node.Node.Guid, outputSlot.IsExecutePin);
            var rightPortRef = new BlueprintPinReference(inputSlot.PortName, inputPort.Node.Node.Guid, inputSlot.IsExecutePin);

            var inRerouteRef = new BlueprintPinReference(PinNames.EXECUTE_IN, reroute.Node.Guid, outputSlot.IsExecutePin);
            var outRerouteRef = new BlueprintPinReference(PinNames.EXECUTE_OUT, reroute.Node.Guid, outputSlot.IsExecutePin);
            DeleteElements(new[] { edgeTarget });
            View.Connect(leftPortRef, inRerouteRef);
            View.Connect(outRerouteRef, rightPortRef);
        }

        public void CreateConverterNode(Edge edgeTarget)
        {
            var outputPort = (BlueprintEditorPort)edgeTarget.output;
            var inputPort = (BlueprintEditorPort)edgeTarget.input;
            
            var outputSlot = edgeTarget.output.GetPin();
            var inputSlot = edgeTarget.input.GetPin();

            if (!View.TryGetConvertMethod(outputSlot.Type, inputSlot.Type, out MethodInfo convertMethod))
            {
                return;
            }

            var assemblyName = convertMethod.DeclaringType?.AssemblyQualifiedName;
            var methodName = convertMethod.Name;
            
            var sr = new SearcherItem("");
            var bse = new BlueprintSearchEntry.Builder().WithFullName("Converter").WithNodeType(BlueprintNodeType.Converter).WithNameData(assemblyName, methodName).Build();
            var pos = Vector2.Lerp(outputPort.worldBound.center, inputPort.worldBound.center, 0.5f) + new Vector2(0, -12);
            sr.UserData = (bse, pos);
            
            View.Select(sr);

            var converter = View.EditorNodes[^1];
            var leftPortRef = new BlueprintPinReference(outputSlot.PortName, outputPort.Node.Node.Guid, outputSlot.IsExecutePin);
            var rightPortRef = new BlueprintPinReference(inputSlot.PortName, inputPort.Node.Node.Guid, inputSlot.IsExecutePin);

            var inRerouteRef = new BlueprintPinReference(PinNames.EXECUTE_IN, converter.Node.Guid, outputSlot.IsExecutePin);
            var outRerouteRef = new BlueprintPinReference(PinNames.RETURN, converter.Node.Guid, outputSlot.IsExecutePin);
            DeleteElements(new[] { edgeTarget });
            View.Connect(leftPortRef, inRerouteRef);
            View.Connect(outRerouteRef, rightPortRef);
        }
        #endregion

        #region - Ports -
        public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
        {
            var allPorts = new List<Port>();
            var validPorts = new List<Port>();

            foreach (var node in View.EditorNodes)
            {
                if (startPort.direction == Direction.Input)
                {
                    allPorts.AddRange(node.OutPorts.Values);
                }
                else
                {
                    allPorts.AddRange(node.InPorts.Values);
                }
            }

            foreach (var p in allPorts)
            {
                if (p == startPort) { continue; }
                if (p.node == startPort.node) { continue; }
                if(IsCompatiblePort(startPort, p))
                {
                    validPorts.Add(p);
                }
            }

            return validPorts;
        }

        public bool IsCompatiblePort(Port startPort, Port p)
        {
            if (p.portType == startPort.portType)
            {
                return true;
            }

            if (p.portType ==  typeof(object) && startPort.portType != typeof(ExecutePin))
            {
                return true;
            }

            if (View.CanConvert(startPort.portType, p.portType))
            {
                return true;
            }
            return false;
        }

        #endregion

        #region - Contextual Menu -

        public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            base.BuildContextualMenu(evt);
            if (evt.target is Edge edge)
            {
                var pos = evt.mousePosition;
                evt.menu.AppendSeparator();
                evt.menu.AppendAction("Create Redirect", _ => CreateRedirectNode(pos, edge), _ => DropdownMenuAction.Status.Normal);
            }
        }

        #endregion
    }
}
