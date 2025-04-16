using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Vapor.Blueprints;
using Vapor.Inspector;
using Assembly = System.Reflection.Assembly;
using Object = UnityEngine.Object;

namespace VaporEditor.Blueprints
{
    public class BlueprintEditorWindow : EditorWindow
    {
        public string SelectedGuid { get; set; }
        
        private BlueprintGraphSo _graphObject;
        public BlueprintGraphSo GraphObject
        {
            get => _graphObject;
            private set
            {
                if (_graphObject != null)
                {
                    DestroyImmediate(_graphObject);
                }

                _graphObject = value;
            }
        }
        
        public BlueprintClassGraphModel ClassGraphModel { get; private set; }
        
        private BlueprintView _graphEditorView;
        public BlueprintView GraphEditorView
        {
            get => _graphEditorView;
            private set
            {
                if (_graphEditorView != null)
                {
                    _graphEditorView.RemoveFromHierarchy();
                    _graphEditorView.Dispose();
                }

                _graphEditorView = value;

                if (_graphEditorView != null)
                {
                    _frameAllAfterLayout = true;
                    // TODO need to supply a viewDataKey for these dimensions
                    SplitView = new TwoPaneSplitView(0, 300, TwoPaneSplitViewOrientation.Horizontal);
                    _blackboardView = new BlueprintBlackboardView(this);
                    var ve = new VisualElement()
                    {
                        style =
                        {
                            flexGrow = 1f,
                        }
                    };
                    _splitView2 = new TwoPaneSplitView(1, 300, TwoPaneSplitViewOrientation.Horizontal);
                    ve.Add(_graphEditorView);
                    _graphEditorView.Blackboard = _blackboardView;

                    _splitView2.Add(ve);
                    _inspectorView = new BlueprintInspectorView(this);
                    _splitView2.Add(_inspectorView);
                    
                    SplitView.Add(_blackboardView);
                    SplitView.Add(_splitView2);
                    rootVisualElement.Add(SplitView);
                }
            }
        }
        
        public BlueprintInspectorView InspectorView => _inspectorView;

        
        public List<BlueprintSearchModel> SearchModels { get; } = new();
        
        [NonSerialized]
        private bool _frameAllAfterLayout;
        [NonSerialized]
        public TwoPaneSplitView SplitView;
        [NonSerialized]
        private TwoPaneSplitView _splitView2;
        [NonSerialized]
        private BlueprintBlackboardView _blackboardView;
        [NonSerialized]
        private BlueprintInspectorView _inspectorView;
        [NonSerialized]
        private BlueprintUndoRedoContainer _undoRedoContainer;
        [NonSerialized]
        private readonly Stack<(object, ChangeType)> _undoStack = new();
        [NonSerialized]
        private readonly Stack<(object, ChangeType)> _redoStack = new();
        [NonSerialized]
        private readonly List<Type> _searchTypes = new();

        public void Initialize(string assetGuid)
        {
            var asset = AssetDatabase.LoadAssetAtPath<BlueprintGraphSo>(AssetDatabase.GUIDToAssetPath(assetGuid));
            if (asset == null)
            {
                Debug.Log("Initialized: Asset null");
                return;
            }

            if (!EditorUtility.IsPersistent(asset))
            {
                Debug.Log("Initialized: Asset not persistant");
                return;
            }

            if (SelectedGuid == assetGuid)
            {
                Debug.Log($"Initialized: Already initialized: {asset.name}");
                return;
            }

            Debug.Log($"Initialized: {asset.GetType()}");

            var path = AssetDatabase.GetAssetPath(asset);
            SelectedGuid = assetGuid;
            string graphName = Path.GetFileNameWithoutExtension(path);

            GraphObject = CreateInstance<BlueprintGraphSo>();
            GraphObject.hideFlags = HideFlags.HideAndDontSave;
            GraphObject.name = graphName;
            EditorUtility.CopySerialized(asset, GraphObject);
            if (GraphObject.GraphJson.EmptyOrNull())
            {
                ClassGraphModel = BlueprintClassGraphModel.New(GraphObject);
                ClassGraphModel.Validate();
            }
            else
            {
                ClassGraphModel = BlueprintClassGraphModel.Load(GraphObject);
            }

            GenerateDefaultSearchTypes();
            UpdateSearchModels();

            ClassGraphModel.VariableChanged += OnClassVariableChanged;
            ClassGraphModel.MethodChanged += OnMethodChanged;
            foreach (var method in ClassGraphModel.Methods)
            {
                method.ArgumentChanged += OnMethodArgumentChanged;
                method.VariableChanged += OnMethodVariableChanged;
                method.NodeChanged += OnMethodNodeChanged;
                method.WireChanged += OnMethodWireChanged;
            }
            _undoRedoContainer = CreateInstance<BlueprintUndoRedoContainer>();
            _undoRedoContainer.hideFlags = HideFlags.HideAndDontSave;
            RecordUndo(ClassGraphModel, ChangeType.Modified);

            GraphEditorView = new BlueprintView(this, ClassGraphModel)
            {
                viewDataKey = SelectedGuid,
            };
            if (!GraphObject.LastOpenedMethod.EmptyOrNull())
            {
                var method = ClassGraphModel.Methods.FirstOrDefault(m => m.MethodName == GraphObject.LastOpenedMethod);
                method?.Edit();
            }

            UpdateTitle();

            Repaint();
        }

        private void GenerateDefaultSearchTypes()
        {
            _searchTypes.Clear();
            var allTypes = new List<Type>(16000);
            allTypes.AddRange(new[]
            {
                typeof(bool),
                typeof(byte),
                typeof(sbyte),
                typeof(char),
                typeof(short),
                typeof(ushort),
                typeof(int),
                typeof(uint),
                typeof(long),
                typeof(ulong),
                typeof(float),
                typeof(double),
                typeof(IntPtr),
                typeof(UIntPtr),
                
                typeof(object),
                typeof(string),
                typeof(Delegate),
            });
            allTypes.AddRange(Assembly.Load("Assembly-CSharp").GetTypes().Where(t => t.IsPublic || t.IsNestedPublic));
            HashSet<string> validNamespaces = new()
            {
                "System.Collections",
                "System.Collections.Generic",
            };

            var precompiledPaths =
                CompilationPipeline.GetPrecompiledAssemblyPaths(CompilationPipeline.PrecompiledAssemblySources.SystemAssembly | CompilationPipeline.PrecompiledAssemblySources.UnityEngine).Select(System.IO.Path.GetFileNameWithoutExtension);
            var precompiledAsms = precompiledPaths.Select(Assembly.Load).Distinct();
            foreach (var precompiledAsm in precompiledAsms)
            {
                if (precompiledAsm.IsDefined(typeof(AssemblyIsEditorAssembly), true))
                {
                    continue;
                }

                allTypes.AddRange(precompiledAsm.GetTypes().Where(t =>
                {
                    if (!(t.IsPublic || t.IsNestedPublic))
                    {
                        return false;
                    }
                    
                    if (t.Name.StartsWith("<"))
                    {
                        return false;
                    }

                    if (t.Namespace == null)
                    {
                        return false;
                    }
                    
                    return validNamespaces.Contains(t.Namespace) || t.Namespace.StartsWith("UnityEngine");
                }));
            }
            
            _searchTypes.Capacity = allTypes.Count;
            foreach (var type in allTypes.Distinct())
            {
                if (type == null)
                {
                    continue;
                }

                if (type.IsSpecialName)
                {
                    continue;
                }

                if (type.IsSubclassOf(typeof(Attribute)))
                {
                    continue;
                }
                
                if (type.IsSubclassOf(typeof(Exception)))
                {
                    continue;
                }
                
                _searchTypes.Add(type);
            }
        }
        public void UpdateSearchModels()
        {
            SearchModels.Clear();
            if (ClassGraphModel == null)
            {
                return;
            }
            
            var allTypes = new List<Type>(16000);
            allTypes.AddRange(_searchTypes);
            HashSet<string> validNamespaces = new();
            foreach (var addNameSpace in ClassGraphModel.Usings)
            {
                validNamespaces.Add(addNameSpace);
            }
            
            var precompiledPaths =
                CompilationPipeline.GetPrecompiledAssemblyPaths(CompilationPipeline.PrecompiledAssemblySources.SystemAssembly | CompilationPipeline.PrecompiledAssemblySources.UnityEngine).Select(System.IO.Path.GetFileNameWithoutExtension);
            var precompiledAsms = precompiledPaths.Select(Assembly.Load).Distinct();
            foreach (var precompiledAsm in precompiledAsms)
            {
                if (precompiledAsm.IsDefined(typeof(AssemblyIsEditorAssembly), true))
                {
                    continue;
                }

                allTypes.AddRange(precompiledAsm.GetTypes().Where(t =>
                {
                    if (!(t.IsPublic || t.IsNestedPublic))
                    {
                        return false;
                    }
                    
                    if (t.Name.StartsWith("<"))
                    {
                        return false;
                    }

                    return t.Namespace != null && validNamespaces.Contains(t.Namespace);
                }));
            }
            
            SearchModels.Capacity += allTypes.Count;
            foreach (var type in allTypes.Distinct())
            {
                if (type == null)
                {
                    continue;
                }

                if (type.IsSpecialName)
                {
                    continue;
                }

                if (type.IsSubclassOf(typeof(Attribute)))
                {
                    continue;
                }
                
                if (type.IsSubclassOf(typeof(Exception)))
                {
                    continue;
                }
                
                // Handle primitive and common types
                string typeName = type.FullName switch
                {
                    "System.Int32" => "int",
                    "System.Boolean" => "bool",
                    "System.Single" => "float",
                    "System.Double" => "double",
                    "System.Int16" => "short",
                    "System.Int64" => "long",
                    "System.Char" => "char",
                    "System.Byte" => "byte",
                    "System.SByte" => "sbyte",
                    "System.UInt16" => "ushort",
                    "System.UInt32" => "uint",
                    "System.UInt64" => "ulong",
                    "System.String" => "string",
                    "System.Object" => "object",
                    "System.Delegate" => "delegate",
                    _ => type.Name // Fallback for custom types
                };
                
                var tn = type.IsGenericType ? $"{typeName.Split('`')[0]}<{string.Join(",", type.GetGenericArguments().Select(a => a.Name))}>" : typeName;
                var bsm = new BlueprintSearchModel($"Types/{type.Namespace?.Replace('.', '/')}", $"{tn}")
                    .WithUserData(type);
                
                SearchModels.Add(bsm);
            }
        }

        public void UpdateTitle()
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(SelectedGuid);
            string assetName = Path.GetFileNameWithoutExtension(assetPath);

            // update blackboard title (before we add suffixes)
            if (GraphEditorView != null)
            {
                GraphEditorView.AssetName = assetName;
            }

            // build the window title (with suffixes)
            string winTitle = assetName;
            if (EditorUtility.IsDirty(GraphObject))
            {
                hasUnsavedChanges = true;
                // This is the message EditorWindow will show when prompting to close while dirty
                saveChangesMessage = GetSaveChangesMessage();
            }
            else
            {
                hasUnsavedChanges = false;
                saveChangesMessage = "";
            }
            if (!AssetFileExists())
            {
                winTitle = winTitle + " (deleted)";
            }

            // get window icon
            Texture2D icon;
            {
                string theme = EditorGUIUtility.isProSkin ? "_dark" : "_light";
                icon = Resources.Load<Texture2D>("Icons/sg_graph_icon_gray" + theme);
            }

            titleContent = new GUIContent(winTitle, icon);
        }
        
        private void OnGeometryChanged(GeometryChangedEvent evt)
        {
            // this callback is only so we can run post-layout behaviors after the graph loads for the first time
            // we immediately unregister it so it doesn't get called again
            if (GraphEditorView == null)
            {
                return;
            }

            if (_frameAllAfterLayout)
            {
                GraphEditorView.FrameAll();
                var vt = GraphEditorView.viewTransform;
                var pos = vt.position + new Vector3(-150, 0, 0); // Account for blackboard, half size
                var scl = vt.scale;
                GraphEditorView.UpdateViewTransform(pos, scl);
            }

            _frameAllAfterLayout = false;
        }
        
        private void OnEnable()
        {
            this.SetAntiAliasing(4);
            Undo.undoRedoEvent -= OnUndoEvent;
            Undo.undoRedoEvent += OnUndoEvent;
        }

        private void OnDisable()
        {
            _graphEditorView?.Dispose();

            _graphEditorView = null;
            GraphObject = null;
            ClassGraphModel = null;
            DestroyImmediate(_undoRedoContainer);
            Undo.undoRedoEvent -= OnUndoEvent;

            Resources.UnloadUnusedAssets();
        }

        

        private void Update()
        {
            try
            {
                if (!GraphObject && SelectedGuid != null)
                {
                    var guid = SelectedGuid;
                    SelectedGuid = null;
                    Initialize(guid);
                }

                if (GraphObject)
                {
                    return;
                }

                Close();
            }
            catch(Exception e)
            {
                Debug.LogWarning(e);
                Close();
                throw;
            }
        }

        #region - Undo / Redo -
        private void OnClassVariableChanged(BlueprintClassGraphModel classGraph, BlueprintVariable variable, ChangeType changeType, bool ignoreUndo)
        {
            if (ignoreUndo)
            {
                return;
            }
            RecordUndo(variable, changeType);
        }

        private void OnMethodChanged(BlueprintClassGraphModel classGraph, BlueprintMethodGraph method, ChangeType changeType, bool ignoreUndo)
        {
            if (ignoreUndo)
            {
                return;
            }
            switch (changeType)
            {
                case ChangeType.Added:
                    RecordUndo(method, changeType);
                    method.ArgumentChanged += OnMethodArgumentChanged;
                    method.VariableChanged += OnMethodVariableChanged;
                    method.NodeChanged += OnMethodNodeChanged;
                    method.WireChanged += OnMethodWireChanged;
                    break;
                case ChangeType.Removed:
                    RecordUndo(method, changeType);
                    method.ArgumentChanged -= OnMethodArgumentChanged;
                    method.VariableChanged -= OnMethodVariableChanged;
                    method.NodeChanged -= OnMethodNodeChanged;
                    method.WireChanged -= OnMethodWireChanged;
                    break;
                case ChangeType.Modified:
                    RecordUndo(method, changeType);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(changeType), changeType, null);
            }
        }

        private void OnMethodArgumentChanged(BlueprintMethodGraph method, BlueprintArgument argument, ChangeType changeType, bool ignoreUndo)
        {
            if (ignoreUndo)
            {
                return;
            }
            RecordUndo(argument, changeType);
        }

        private void OnMethodVariableChanged(BlueprintMethodGraph method, BlueprintVariable variable, ChangeType changeType, bool ignoreUndo)
        {
            if (ignoreUndo)
            {
                return;
            }
            RecordUndo(variable, changeType);
        }

        private void OnMethodNodeChanged(BlueprintMethodGraph method, NodeModelBase node, ChangeType changeType, bool ignoreUndo)
        {
            if (ignoreUndo)
            {
                return;
            }
            RecordUndo(node, changeType);
        }

        private void OnMethodWireChanged(BlueprintMethodGraph method, BlueprintWire wire, ChangeType changeType, bool ignoreUndo)
        {
            if (ignoreUndo)
            {
                return;
            }
            RecordUndo(wire, changeType);
        }

        private void OnUndoEvent(in UndoRedoInfo undoInfo)
        {
            if (!_undoRedoContainer)
            {
                return;
            }

            if (undoInfo.isRedo && _redoStack.TryPop(out var redo))
            {
                Debug.Log($"Redo: {redo}");
                switch (redo.Item2)
                {
                    case ChangeType.Added:
                    {
                        // Add
                        if (redo.Item1 is IBlueprintGraphModel)
                        {
                            switch (redo.Item1)
                            {
                                case BlueprintClassGraphModel classGraph:
                                    break;
                                case BlueprintMethodGraph methodGraph:
                                {
                                    methodGraph.ClassGraphModel.AddMethod(methodGraph, true);
                                    break;
                                }
                                case BlueprintVariable variable:
                                {
                                    if (variable.Scope == VariableScopeType.Method)
                                    {
                                        variable.MethodGraph.AddVariable(variable, true);
                                    }
                                    else
                                    {
                                        variable.ClassGraphModel.AddVariable(variable, true);
                                    }

                                    break;
                                }
                                case BlueprintArgument argument:
                                {
                                    argument.Method.AddArgument(argument, true);
                                    break;
                                }
                                case NodeModelBase node:
                                {
                                    node.Method.AddNode(node, true);
                                    break;
                                }
                                case BlueprintWire wire:
                                {
                                    wire.Method.AddWire(wire, true);
                                    break;
                                }
                            }
                        }

                        if (redo.Item1 is List<IBlueprintGraphModel> models)
                        {

                        }
                        break;
                    }
                    case ChangeType.Removed:
                    {
                        // Remove
                        if (redo.Item1 is IBlueprintGraphModel)
                        {
                            switch (redo.Item1)
                            {
                                case BlueprintClassGraphModel classGraph:
                                    break;
                                case BlueprintMethodGraph methodGraph:
                                {
                                    methodGraph.ClassGraphModel.RemoveMethod(methodGraph, true);
                                    break;
                                }
                                case BlueprintVariable variable:
                                {
                                    if (variable.Scope == VariableScopeType.Method)
                                    {
                                        variable.MethodGraph.RemoveVariable(variable, true);
                                    }
                                    else
                                    {
                                        variable.ClassGraphModel.RemoveVariable(variable, true);
                                    }

                                    break;
                                }
                                case BlueprintArgument argument:
                                {
                                    argument.Method.RemoveArgument(argument, true);
                                    break;
                                }
                                case NodeModelBase node:
                                {
                                    node.Method.RemoveNode(node, true);
                                    break;
                                }
                                case BlueprintWire wire:
                                {
                                    wire.Method.RemoveWire(wire, true);
                                    break;
                                }
                            }
                        }

                        if (redo.Item1 is List<IBlueprintGraphModel> models)
                        {

                        }
                        break;
                    }
                    case ChangeType.Modified:
                        // Replace
                        break;
                }
                
                _undoStack.Push(redo);
            }
            else if(_undoStack.TryPop(out var undo))
            {
                Debug.Log($"Undo: {undo}");
                switch (undo.Item2)
                {
                    case ChangeType.Added:
                    {
                        // Remove
                        if (undo.Item1 is IBlueprintGraphModel)
                        {
                            switch (undo.Item1)
                            {
                                case BlueprintClassGraphModel classGraph:
                                    break;
                                case BlueprintMethodGraph methodGraph:
                                {
                                    methodGraph.ClassGraphModel.RemoveMethod(methodGraph, true);
                                    break;
                                }
                                case BlueprintVariable variable:
                                {
                                    if (variable.Scope == VariableScopeType.Method)
                                    {
                                        variable.MethodGraph.RemoveVariable(variable, true);
                                    }
                                    else
                                    {
                                        variable.ClassGraphModel.RemoveVariable(variable, true);
                                    }

                                    break;
                                }
                                case BlueprintArgument argument:
                                {
                                    argument.Method.RemoveArgument(argument, true);
                                    break;
                                }
                                case NodeModelBase node:
                                {
                                    node.Method.RemoveNode(node, true);
                                    break;
                                }
                                case BlueprintWire wire:
                                {
                                    wire.Method.RemoveWire(wire, true);
                                    break;
                                }
                            }
                        }

                        if (undo.Item1 is List<IBlueprintGraphModel> models)
                        {

                        }

                        break;
                    }
                    case ChangeType.Removed:
                    {
                        // Add
                        if (undo.Item1 is IBlueprintGraphModel)
                        {
                            switch (undo.Item1)
                            {
                                case BlueprintClassGraphModel classGraph:
                                    break;
                                case BlueprintMethodGraph methodGraph:
                                {
                                    methodGraph.ClassGraphModel.AddMethod(methodGraph, true);
                                    break;
                                }
                                case BlueprintVariable variable:
                                {
                                    if (variable.Scope == VariableScopeType.Method)
                                    {
                                        variable.MethodGraph.AddVariable(variable, true);
                                    }
                                    else
                                    {
                                        variable.ClassGraphModel.AddVariable(variable, true);
                                    }

                                    break;
                                }
                                case BlueprintArgument argument:
                                {
                                    argument.Method.AddArgument(argument, true);
                                    break;
                                }
                                case NodeModelBase node:
                                {
                                    node.Method.AddNode(node, true);
                                    break;
                                }
                                case BlueprintWire wire:
                                {
                                    wire.Method.AddWire(wire, true);
                                    break;
                                }
                            }
                        }

                        if (undo.Item1 is List<IBlueprintGraphModel> models)
                        {

                        }

                        break;
                    }
                    case ChangeType.Modified:
                        // Replace
                        break;
                }
                
                _redoStack.Push(undo);
            }
        }

        public void RecordUndo(IBlueprintGraphModel model, ChangeType changeType)
        {
            if (!_undoRedoContainer)
            {
                return;
            }

            Undo.RecordObject(_undoRedoContainer, changeType.ToString());
            _undoRedoContainer.Set(Guid.NewGuid().ToString(), changeType);
            _undoStack.Push((model, changeType));
        }

        public void RecordUndo(List<IBlueprintGraphModel> model, ChangeType changeType)
        {
            if (!_undoRedoContainer)
            {
                return;
            }

            Undo.RecordObject(_undoRedoContainer, changeType.ToString());
            _undoRedoContainer.Set(Guid.NewGuid().ToString(), changeType);
            _undoStack.Push((model, changeType));
        }
        #endregion
        
        #region - Helpers -
        private bool AssetFileExists()
        {
            var assetPath = AssetDatabase.GUIDToAssetPath(SelectedGuid);
            return File.Exists(assetPath);
        }

        public void SaveAsset()
        {
            if (SelectedGuid != null && GraphObject)
            {
                Debug.Log("Save Called");
                var mainAsset = AssetDatabase.LoadAssetAtPath<BlueprintGraphSo>(AssetDatabase.GUIDToAssetPath(SelectedGuid));
                ClassGraphModel.Validate();
                var designGraphJson = ClassGraphModel?.Serialize();
                GraphObject.GraphJson = designGraphJson;
                mainAsset.GraphJson = GraphObject.GraphJson;
                mainAsset.Version++;
                mainAsset.LastOpenedMethod = GraphObject.LastOpenedMethod;
                
                // EditorUtility.CopySerialized(GraphObject, mainAsset);

                EditorUtility.SetDirty(mainAsset);
                AssetDatabase.SaveAssetIfDirty(mainAsset);

                EditorUtility.ClearDirty(GraphObject);
                hasUnsavedChanges = false;
            }

            UpdateTitle();
        }

        // public void CompileAsset()
        // {
        //     if (SelectedGuid != null && GraphObject)
        //     {
        //         var mainAsset = AssetDatabase.LoadAssetAtPath<BlueprintGraphSo>(AssetDatabase.GUIDToAssetPath(SelectedGuid));
        //         GraphObject.CompileGraph();
        //         mainAsset.CompiledGraphJson = GraphObject.CompiledGraphJson;
        //
        //         EditorUtility.SetDirty(mainAsset);
        //         AssetDatabase.SaveAssetIfDirty(mainAsset);
        //     }
        // }

        public void CompileToScript()
        {
            if (SelectedGuid != null && GraphObject)
            {
                if (!ClassGraphModel.Validate())
                {
                    Debug.LogError("Unable To Validate Graph");
                    return;
                }
                
                //TODO Create a BlueprintScriptCompiler, that takes the design graph and creates a class from it.
                var mainAsset = AssetDatabase.LoadAssetAtPath<BlueprintGraphSo>(AssetDatabase.GUIDToAssetPath(SelectedGuid));
                var path = BlueprintScriptWriter.GetFullCSharpPath(mainAsset);
                if (path.EmptyOrNull())
                {
                    Debug.LogError("Invalid CSharp Path");
                    return;
                }

                SaveAsset();
                BlueprintScriptWriter.WriteScript(GraphObject, ClassGraphModel, path);
                AssetDatabase.Refresh();
            }
        }

        public void PingAsset()
        {
            if (SelectedGuid == null)
            {
                return;
            }

            var path = AssetDatabase.GUIDToAssetPath(SelectedGuid);
            var asset = AssetDatabase.LoadAssetAtPath<Object>(path);
            EditorGUIUtility.PingObject(asset);
        }

        public void SetBlackboardVisibility(bool visible)
        {
            if (visible)
            {
                SplitView.UnCollapse();
            }
            else
            {
                SplitView.CollapseChild(0);
            }
        }

        public void SetInspectorVisibility(bool visible)
        {
            if (visible)
            {
                _splitView2.UnCollapse();
            }
            else
            {
                _splitView2.CollapseChild(1);
            }
        }

        private string GetSaveChangesMessage()
        {
            return "Do you want to save the changes you made in the Graph?\n\n" +
                   AssetDatabase.GUIDToAssetPath(SelectedGuid) +
                   "\n\nYour changes will be lost if you don't save them.";
        }

        public void MarkDirty()
        {
            EditorUtility.SetDirty(GraphObject);
            hasUnsavedChanges = true;
        }
        #endregion
    }
}
