//using System;
//using UnityEngine.UIElements;
//using Vapor.UI;

//namespace VaporEditor.Graphs
//{
//    public class BlackboardCategoryViewModel : IBPViewModel
//    {
//        public VisualElement ParentView { get; set; }
//        public string Name { get; set; }
//        public string AssociatedCategoryGuid { get; set; }
//        public bool IsExpanded { get; set; }
//        public Action<ISourceDataAction> RequestModelChangeAction { get; set; }

//        // Wipes all data in this view-model
//        public void ResetViewModelData()
//        {
//            Name = string.Empty;
//            AssociatedCategoryGuid = string.Empty;
//            IsExpanded = false;
//            RequestModelChangeAction = null;
//        }
//    }
//}
