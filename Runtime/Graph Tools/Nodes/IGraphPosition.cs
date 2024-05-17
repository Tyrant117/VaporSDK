using UnityEngine;

namespace Vapor.GraphTools
{
    public interface IGraphPosition
    {
        /// <summary>
        /// Gets or sets the position of the node in a graph.
        /// </summary>
        Rect Position { get; set; }
    }
}
