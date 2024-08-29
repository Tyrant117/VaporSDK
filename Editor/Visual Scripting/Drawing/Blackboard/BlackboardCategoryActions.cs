//using System;
//using System.Collections.Generic;
//using UnityEditor;
//using UnityEngine;
//using UnityEngine.Assertions;
//using Vapor.UI;

//namespace VaporEditor.Graphs
//{
//    public class MoveShaderInputAction : ISourceDataAction
//    {
//        void MoveShaderInput(object graphData)
//        {
//            Assert.IsNotNull(graphData, "GraphData is null while carrying out MoveShaderInputAction");
//            Assert.IsNotNull(ShaderInputReference, "ShaderInputReference is null while carrying out MoveShaderInputAction");
//            graphData.owner.RegisterCompleteObjectUndo("Move Graph Input");
//            graphData.MoveItemInCategory(ShaderInputReference, NewIndexValue, AssociatedCategoryGuid);
//        }

//        public Action<object> ModifyGraphDataAction => MoveShaderInput;

//        internal string AssociatedCategoryGuid { get; set; }

//        // Reference to the shader input being modified
//        internal ShaderInput ShaderInputReference { get; set; }

//        internal int NewIndexValue { get; set; }
//    }

//    public class DeleteCategoryAction : ISourceDataAction
//    {
//        void RemoveCategory(GraphData graphData)
//        {
//            Assert.IsNotNull(graphData, "GraphData is null while carrying out DeleteCategoryAction");
//            Assert.IsNotNull(categoriesToRemoveGuids, "CategoryToRemove is null while carrying out DeleteCategoryAction");

//            // This is called by MaterialGraphView currently, no need to repeat it here, though ideally it would live here
//            //graphData.owner.RegisterCompleteObjectUndo("Delete Category");

//            foreach (var categoryGUID in categoriesToRemoveGuids)
//            {
//                graphData.RemoveCategory(categoryGUID);
//            }
//        }

//        public Action<GraphData> ModifyGraphDataAction => RemoveCategory;

//        // Reference to the guid(s) of categories being deleted
//        public HashSet<string> categoriesToRemoveGuids { get; set; } = new HashSet<string>();
//    }

//    public class ChangeCategoryIsExpandedAction : ISourceDataAction
//    {
//        public const string s_KEditorPrefKey = ".isCategoryExpanded";

//        void ChangeIsExpanded(object graphData)
//        {
//            Assert.IsNotNull(graphData, "GraphData is null while carrying out ChangeIsExpanded on Category");
//            foreach (var catid in CategoryGuids)
//            {
//                var key = $"{editorPrefsBaseKey}.{catid}.{s_KEditorPrefKey}";
//                var currentValue = EditorPrefs.GetBool(key, true);

//                if (currentValue != IsExpanded)
//                {
//                    EditorPrefs.SetBool(key, IsExpanded);
//                }
//            }
//        }

//        public string editorPrefsBaseKey;
//        public List<string> CategoryGuids { get; set; }
//        public bool IsExpanded { get; set; }

//        public Action<object> ModifyGraphDataAction => ChangeIsExpanded;
//    }

//    public class ChangeCategoryNameAction : ISourceDataAction
//    {
//        void ChangeCategoryName(object graphData)
//        {
//            Assert.IsNotNull(graphData, "GraphData is null while carrying out ChangeCategoryNameAction");
//            graphData.owner.RegisterCompleteObjectUndo("Change Category Name");
//            graphData.ChangeCategoryName(CategoryGuid, NewCategoryNameValue);
//        }

//        public Action<object> ModifyGraphDataAction => ChangeCategoryName;

//        // Guid of the category being modified
//        public string CategoryGuid { get; set; }

//        public string NewCategoryNameValue { get; set; }
//    }
//}
