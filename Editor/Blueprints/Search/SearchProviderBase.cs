using System;
using System.Collections.Generic;
using UnityEngine;

namespace VaporEditor.Blueprints
{
    public abstract class SearchProviderBase
    {
        public Vector2 Position { get; set; }
        public BlueprintSearchWindow SearchWindow { get; set; }

        public abstract bool Select(BlueprintSearchModel model);
            
        public abstract IEnumerable<BlueprintSearchModel> GetDescriptors();
    }
}