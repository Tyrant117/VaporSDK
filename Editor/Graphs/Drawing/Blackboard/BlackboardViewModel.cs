//using System;
//using System.Collections.Generic;
//using UnityEngine.UIElements;
//using Vapor.UI;

//namespace VaporEditor.Graphs
//{
//    public class CategoryData { }

//    public class BlackboardViewModel : IBPViewModel
//    {
//        public GraphObject Model { get; set; }
//        public VisualElement ParentView { get; set; }
//        public string Title { get; set; }
//        public string Subtitle { get; set; }
//        public Dictionary<string, ISourceDataAction> PropertyNameToAddActionMap { get; set; }
//        public Dictionary<string, ISourceDataAction> DefaultKeywordNameToAddActionMap { get; set; }
//        public Dictionary<string, ISourceDataAction> BuiltInKeywordNameToAddActionMap { get; set; }
//        public Tuple<string, ISourceDataAction> DefaultDropdownNameToAdd { get; set; }

//        public ISourceDataAction AddCategoryAction { get; set; }
//        public Action<ISourceDataAction> RequestModelChangeAction { get; set; }
//        public List<CategoryData> CategoryInfoList { get; set; }

//        // Can't add disbled keywords, so don't need an add action
//        public List<string> DisabledKeywordNameList { get; set; }
//        public List<string> DisabledDropdownNameList { get; set; }

//        public BlackboardViewModel()
//        {
//            PropertyNameToAddActionMap = new Dictionary<string, ISourceDataAction>();
//            DefaultKeywordNameToAddActionMap = new Dictionary<string, ISourceDataAction>();
//            BuiltInKeywordNameToAddActionMap = new Dictionary<string, ISourceDataAction>();
//            DefaultDropdownNameToAdd = null;
//            CategoryInfoList = new List<CategoryData>();
//            DisabledKeywordNameList = new List<string>();
//            DisabledDropdownNameList = new List<string>();
//        }

//        public void ResetViewModelData()
//        {
//            Subtitle = string.Empty;
//            PropertyNameToAddActionMap.Clear();
//            DefaultKeywordNameToAddActionMap.Clear();
//            BuiltInKeywordNameToAddActionMap.Clear();
//            DefaultDropdownNameToAdd = null;
//            CategoryInfoList.Clear();
//            DisabledKeywordNameList.Clear();
//            DisabledDropdownNameList.Clear();
//            RequestModelChangeAction = null;
//        }
//    }
//}