using UnityEditor.Experimental.GraphView;

namespace VaporEditor.VisualScripting
{
    public interface IBPResizable : IResizable
    {
        // Depending on the return value, the ElementResizer either allows resizing past parent view edge (like in case of StickyNote) or clamps the size at the edges of parent view (like for GraphSubWindows)
        bool CanResizePastParentBounds();
    }
}
