using UnityEngine;
using Vapor.VisualScripting;
using Vapor.Inspector;
using VaporEditor.VisualScripting;

namespace VaporEditor
{
    public class InspectorViewController : UIViewController<GraphModel, InspectorViewModel>
    {
        public InspectorView View { get; set; }

        public InspectorViewController(GraphModel model, InspectorViewModel viewModel, ISourceDataStore<GraphModel> graphDataStore) : base(model, viewModel, graphDataStore)
        {
            View = new InspectorView(ViewModel, this);
        }

        protected override void ModelChanged(GraphModel graphData, ISourceDataAction changeAction)
        {
            // Lets all event handlers this controller owns/manages know that the model has changed
            // Usually this is to update views and make them reconstruct themself from updated view-model
            NotifyChange(changeAction);

            // Let child controllers know about changes to this controller so they may update themselves in turn
            ApplyChanges();
        }

        protected override void RequestModelChange(ISourceDataAction changeAction)
        {
            DataStore.Dispatch(changeAction);
        }

        public override void Dispose()
        {
            View.Dispose();
            base.Dispose();
        }
    }
}
