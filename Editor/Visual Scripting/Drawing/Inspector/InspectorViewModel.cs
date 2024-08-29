using UnityEngine.UIElements;

namespace VaporEditor.VisualScripting
{
    public class InspectorViewModel : IBPViewModel
    {
        public VisualElement ParentView { get; set; }

        public void ResetViewModelData()
        {
        }
    }
}