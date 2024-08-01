using System;
using System.IO;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Vapor.Graphs;
using Object = UnityEngine.Object;

namespace VaporEditor.Graphs
{
    public class GraphEditorWindow : EditorWindow
    {
        public string SelectedGuid { get; internal set; }
        private GraphObject _graphObject;
        public GraphObject GraphObject
        {
            get { return _graphObject; }
            set
            {
                if (_graphObject != null)
                    DestroyImmediate(_graphObject);
                _graphObject = value;
            }
        }

        private GraphEditorView _graphEditorView;
        public GraphEditorView GraphEditorView
        {
            get { return _graphEditorView; }
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
                    //_graphEditorView.saveRequested += () => SaveAsset();
                    //_graphEditorView.saveAsRequested += SaveAs;
                    //_graphEditorView.convertToSubgraphRequested += ToSubGraph;
                    //_graphEditorView.showInProjectRequested += PingAsset;
                    //_graphEditorView.isCheckedOut += IsGraphAssetCheckedOut;
                    //_graphEditorView.checkOut += CheckoutAsset;
                    _graphEditorView.RegisterCallbackOnce<GeometryChangedEvent>(OnGeometryChanged);
                    _frameAllAfterLayout = true;
                    rootVisualElement.Add(_graphEditorView);
                }
            }
        }


        private string _lastSerializedFileContents;
        [NonSerialized]
        private bool _frameAllAfterLayout;

        public void Initialize(string assetGuid)
        {
            var asset = AssetDatabase.LoadAssetAtPath<GraphSo>(AssetDatabase.GUIDToAssetPath(assetGuid));
            if (asset == null)
            {
                Debug.Log("Initialized: Asset null");
                return;
            }

            if (!EditorUtility.IsPersistent(asset))
            {
                Debug.Log("Initialized: Asset not peristant");
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

            _lastSerializedFileContents = asset.JsonGraph;// File.ReadAllText(path);
            var graph = JsonUtility.FromJson<Graph>(_lastSerializedFileContents);
            GraphObject = CreateInstance<GraphObject>();
            GraphObject.hideFlags = HideFlags.HideAndDontSave;
            GraphObject.Setup(graph);
            GraphObject.Validate();

            GraphEditorView = new (asset.SearchIncludeFlags)
            {
                viewDataKey = SelectedGuid,
            };

            //CreateEditorView(asset, graphName);

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
            string title = assetName;
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
                title = title + " (deleted)";
            }

            // get window icon
            Texture2D icon;
            {
                string theme = EditorGUIUtility.isProSkin ? "_dark" : "_light";
                icon = Resources.Load<Texture2D>("Icons/sg_graph_icon_gray" + theme);
            }

            titleContent = new GUIContent(title, icon);
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
                GraphEditorView?.FrameAll();
                //GraphEditorView?.GraphView?.FrameAll();
            }

            _frameAllAfterLayout = false;
        }

        private void OnEnable()
        {
            this.SetAntiAliasing(4);
        }

        private void OnDisable()
        {
            _graphEditorView?.UnregisterCallback<GeometryChangedEvent>(OnGeometryChanged);
            _graphEditorView?.Dispose();

            _graphEditorView = null;
            GraphObject = null;

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
            }
            catch(Exception e)
            {
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
