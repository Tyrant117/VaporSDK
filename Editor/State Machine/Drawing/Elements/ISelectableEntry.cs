using UnityEngine;

namespace VaporEditor.StateMachines
{
    public interface ISelectableEntry
    {
        void Select();
        void Deselect();
    }
}
