using System;
using System.IO;
using Newtonsoft.Json;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Vapor.Blueprints;
using Vapor.Inspector;
using Vapor.NewtonsoftConverters;
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
        
        public BlueprintDesignGraph DesignGraph { get; private set; }
        
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
                DesignGraph = new BlueprintDesignGraph(GraphObject, new BlueprintDesignGraphDto());
                DesignGraph.Validate();
            }
            else
            {
                var dto = JsonConvert.DeserializeObject<BlueprintDesignGraphDto>(GraphObject.GraphJson, NewtonsoftUtility.SerializerSettings);
                DesignGraph = new BlueprintDesignGraph(GraphObject, dto);
            }
            // GraphObject.OpenGraph();

            GraphEditorView = new BlueprintView(this, DesignGraph)
            {
                viewDataKey = SelectedGuid,
            };

            UpdateTitle();

            Repaint();
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
        }

        private void OnDisable()
        {
            _graphEditorView?.Dispose();

            _graphEditorView = null;
            GraphObject = null;
            DesignGraph = null;

            Resources.UnloadUnusedAssets();
        }
        
        private void Update()
        {
            try
            {
                if (GraphObject == null && SelectedGuid != null)
                {
                    var guid = SelectedGuid;
                    SelectedGuid = null;
                    Initialize(guid);
                }

                if (GraphObject == null)
                {
                    Close();
                    return;
                }
            }
            catch(Exception e)
            {
                _graphEditorView = null;
                GraphObject = null;
                DesignGraph = null;
                Debug.LogException(e);
                throw;
            }
        }
        
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
                DesignGraph.Validate();
                var designGraphJson = DesignGraph?.Serialize();
                GraphObject.GraphJson = designGraphJson;
                mainAsset.GraphJson = GraphObject.GraphJson;
                
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
                if (!DesignGraph.Validate())
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
                BlueprintScriptWriter.WriteScript(GraphObject, DesignGraph, path);
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
