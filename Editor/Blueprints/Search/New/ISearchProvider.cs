using System.Collections.Generic;
using UnityEngine;

namespace VaporEditor.Blueprints
{
    internal interface ISearchProvider
    {
        IEnumerable<BlueprintSearchModel> GetDescriptors();

        void AddNode(BlueprintSearchWindow.Descriptor descriptor);

        Vector2 Position { get; set; }
    }
}