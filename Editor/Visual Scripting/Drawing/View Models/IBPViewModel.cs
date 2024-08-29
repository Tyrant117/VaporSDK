using UnityEngine.UIElements;

namespace VaporEditor.VisualScripting
{
    public interface IBPViewModel
    {
        VisualElement ParentView { get; set; }

        // Wipes all data in this view-model that will be fed to the view that depends on it
        // A notable point is that this function typically should not null out the parentView field seen above
        void ResetViewModelData();
    }
}
