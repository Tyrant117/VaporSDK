namespace VaporEditor.Blueprints
{
    public enum GraphInvalidationType
    {
        RenamedNode, // Revaluates all node names
        RetypedNode, // Reavaluates node types
        Topology, // Means either nodes were added or removed
        Graph, // Means the graph needs to be reloaded
    }
}