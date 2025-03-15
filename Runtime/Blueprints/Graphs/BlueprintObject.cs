using UnityEngine;

namespace Vapor.Blueprints
{
    [Blueprintable]
    public abstract class BlueprintObject
    {
        public virtual void SpawnObject(GameObject gameObject)
        {
        }
    }
}
