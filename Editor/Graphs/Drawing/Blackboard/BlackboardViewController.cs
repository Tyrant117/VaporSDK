//using System.Collections.Generic;
//using UnityEditor.Experimental.GraphView;
//using UnityEngine;
//using Vapor.Graphs;
//using Vapor.UI;
//using VaporEditor.Graphs;

//namespace VaporEditor.Graphs
//{
//    public class BlackboardViewController : UIViewController<GraphModel, BlackboardViewModel>
//    {
//        public BlackboardViewController(GraphModel model, BlackboardViewModel viewModel, GraphObject graphObject) : base(model, viewModel, graphObject)
//        {

//        }

//        #region - Model Update -
//        protected override void RequestModelChange(ISourceDataAction changeAction)
//        {
//            DataStore.Dispatch(changeAction);
//        }

//        protected override void ModelChanged(object graphData, ISourceDataAction changeAction)
//        {

//        }
//        #endregion

//        #region - Drawing -
//        public void UpdateBlackboardTitle(string newTitle)
//        {
//            ViewModel.Title = newTitle;
//            Blackboard.title = ViewModel.Title;
//        }
//        #endregion

//        #region - Sections -

//        #endregion

//        #region - Rows -

//        #endregion

//        #region - Fields -

//        #endregion
//    }
//}
