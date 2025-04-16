using UnityEngine;
using Vapor.Blueprints;

namespace VaporEditor.Blueprints
{
    [System.Serializable]
    public struct BlueprintUndoRedoInfo
    {
        public string Id;
        public ChangeType ChangeType;
    }
    public class BlueprintUndoRedoContainer : ScriptableObject
    {
        public BlueprintUndoRedoInfo LastUndoRedoInfo;

        public void Set(string id, ChangeType modified)
        {
            LastUndoRedoInfo = new BlueprintUndoRedoInfo { Id = id, ChangeType = modified };
        }
    }
}
