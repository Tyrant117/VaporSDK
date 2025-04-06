using System;
using System.Collections.Generic;
using UnityEngine;

namespace VaporEditor.Blueprints
{
    public abstract class SearchProviderBase : ISearchProvider
    {
        public Vector2 Position { get; set; }
            
        private readonly Action<BlueprintSearchModel, Vector2> _spawnNode;

        public SearchProviderBase(Action<BlueprintSearchModel, Vector2> onSpawnNode)
        {
            _spawnNode = onSpawnNode;
        }

        public void AddNode(BlueprintSearchWindow.Descriptor descriptor)
        {
            _spawnNode.Invoke(descriptor.SearchModel, Position);
        }
            
        public abstract IEnumerable<BlueprintSearchModel> GetDescriptors();
    }
}