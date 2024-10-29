using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Vapor.Inspector;

namespace Vapor.VisualScripting
{
    public class FunctionGraph : IGraph
    {
        public uint Id { get; }

        public readonly FunctionEntryNode Entry;

        public readonly Dictionary<string, object> InputData;
        public readonly Dictionary<string, object> OutputData;

        private readonly Dictionary<string, object> _tempData;

        public FunctionGraph(INode entry)
        {
            Entry = (FunctionEntryNode)entry;
            InputData = new Dictionary<string, object>();
            OutputData = new Dictionary<string, object>();
            _tempData = new Dictionary<string, object>();

            Traverse(SetGraph);
        }

        public void Evaluate(IGraphOwner graphOwner, params ValueTuple<string,object>[] parameters)
        {            
            InputData.Clear();
            foreach (var param in parameters)
            {
                InputData.Add(param.Item1, param.Item2);
            }
            Evaluate(graphOwner);
        }

        public void Evaluate(IGraphOwner graphOwner)
        {
            _tempData.Clear();
            Entry.Invoke(graphOwner);
        }

        public T GetInputValue<T>(string fieldName) => InputData.TryGetValue(fieldName, out var data) ? (T)data : default;
        public void SetReturnData(string fieldName, object data) => OutputData[fieldName] = data;
        public T GetReturnValue<T>(string fieldName) => OutputData.TryGetValue(fieldName, out var data) ? (T)data : default;

        public void SetTempData(string fieldName, object data) => _tempData[fieldName] = data;
        public T GetTempData<T>(string fieldName) => _tempData.TryGetValue(fieldName, out var data) ? (T)data : default;

        public void Traverse(Action<INode> callback)
        {
            Entry.Traverse(callback);
        }

        private void SetGraph(INode node)
        {
            node.Graph = this;
        }
    }

    [Serializable, DrawWithVapor(UIGroupType.Vertical)]
    public class GraphFieldEntry
    {
        [HorizontalGroup("H"), OnValueChanged("OnChanged", false)]
        public string FieldName = string.Empty;
        [HorizontalGroup("H"), TypeSelector("@GetTypes"), OnValueChanged("OnChanged", false)]
        public string FieldType = typeof(double).AssemblyQualifiedName;

        [NonSerialized]
        public Action Changed;
        private void OnChanged(string old, string @new)
        {
            Changed?.Invoke();
        }

        public static IEnumerable<Type> GetTypes()
        {
            return new List<Type>()
            {
                typeof(double),
                typeof(bool),
                typeof(int),
            };
        }

        public (string, Type) ToTuple()
        {
            return FieldName == null || FieldType == null ? (string.Empty, typeof(double)) : (FieldName, Type.GetType(FieldType));
        }
    }

    [Serializable]
    public class FunctionGraphModel : GraphModel
    {
        [Serializable]
        public class InspectorDrawer
        {
            [OnValueChanged("OnInputChanged", false)]
            public List<GraphFieldEntry> Input;
            [OnValueChanged("OnOutputChanged", false)]
            public List<GraphFieldEntry> Output;

            public FunctionGraphModel Graph { get; set; }
            [JsonIgnore]
            private readonly List<(string, Type)> _cachedReturnTuples = new();

            private void OnInputChanged(int old, int current)
            {
                foreach (var item in Input)
                {
                    item.Changed = OnInputFieldChanged;
                }

                OnInputFieldChanged();
            }

            private void OnOutputChanged(int old, int current)
            {
                foreach (var item in Output)
                {
                    item.Changed = OnOutputFieldChanged;
                }

                OnOutputFieldChanged();
            }

            private void OnInputFieldChanged()
            {
                _cachedReturnTuples.Clear();
                foreach (var item in Input)
                {
                    _cachedReturnTuples.Add(item.ToTuple());
                }
                Graph.GetEntryNode().UpdateInputValues(_cachedReturnTuples);
                Graph.OnRedrawEntryNode();
            }

            private void OnOutputFieldChanged()
            {
                _cachedReturnTuples.Clear();
                foreach (var item in Output)
                {
                    _cachedReturnTuples.Add(item.ToTuple());
                }
                Graph.GetReturnNode().UpdateReturnValues(_cachedReturnTuples);
                Graph.OnRedrawReturnNode();
            }
        }

        public InspectorDrawer Inspector;

        [JsonIgnore]
        public Action<NodeModel> RedrawEntryNode;
        [JsonIgnore]
        public Action<NodeModel> RedrawReturnNode;

        public FunctionGraphModel()
        {
            AssemblyQualifiedType = GetType();
        }

        public override IGraph Build(bool refresh = false, string debugName = "")
        {
            DebugName = debugName;
            if (refresh)
            {
                foreach (var c in Nodes)
                {
                    c.Refresh();
                }
            }

            var entry = GetEntryNode();
            var exit = GetReturnNode();
            var root = entry.Build(this);

            return new FunctionGraph(root);

            //// Define the type parameter
            //Type typeParameter = Type.GetType(Inspector.Output[0].FieldType);

            //// Define the type of the class to create
            //Type genericType = typeof(FunctionGraph).MakeGenericType(typeParameter);

            //// Create an instance of FunctionGraph2<MyStruct>
            //return (IGraph)Activator.CreateInstance(genericType, root);
        }

        public FunctionEntryNodeModel GetEntryNode()
        {
            var entry = Nodes.OfType<FunctionEntryNodeModel>().FirstOrDefault();
            if (entry == null)
            {
                entry = GenerateDefaultEntryNode();
                Nodes.Add(entry);
            }
            return entry;
        }

        public void OnRedrawEntryNode()
        {
            RedrawEntryNode?.Invoke(GetEntryNode());
        }

        public FunctionReturnNodeModel GetReturnNode()
        {
            var @return = Nodes.OfType<FunctionReturnNodeModel>().FirstOrDefault();
            if (@return == null)
            {
                @return = GenerateDefaultReturnNode();
                Nodes.Add(@return);
            }
            return @return;
        }

        public void OnRedrawReturnNode()
        {
            RedrawReturnNode?.Invoke(GetReturnNode());
        }

        public virtual FunctionEntryNodeModel GenerateDefaultEntryNode()
        {
            var entry = new FunctionEntryNodeModel()
            {
                Guid = NodeModel.CreateGuid(),
                NodeType = typeof(FunctionEntryNodeModel).AssemblyQualifiedName,
                Position = new Rect(-100, 0, 0, 0)
            };
            return entry;
        }

        public virtual FunctionReturnNodeModel GenerateDefaultReturnNode()
        {
            var exit = new FunctionReturnNodeModel()
            {
                Guid = NodeModel.CreateGuid(),
                NodeType = typeof(FunctionReturnNodeModel).AssemblyQualifiedName,
                Position = new Rect(100, 0, 0, 0)
            };
            return exit;
        }

        public override object GraphSettingsInspector()
        {
            if (Inspector != null)
            {
                Inspector.Graph = this;
                return Inspector;
            }
            Inspector = new InspectorDrawer
            {
                Graph = this
            };

            return Inspector;
        }
    }
}
