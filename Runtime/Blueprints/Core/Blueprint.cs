using System;
using UnityEngine;
using Vapor.Keys;

namespace Vapor.Blueprints
{
    public static class Blueprint
    {
        public static T New<T>(int blueprintId) where T : IBlueprintableObject, new()
        {
            var newBlueprint = CreateBlueprintObject<T>(blueprintId);
#if UNITY_EDITOR
            var graph = RuntimeDatabase<BlueprintGraphSo>.Get(blueprintId);
            var pm = PlayModeClass.Create(graph);
            newBlueprint.PlayMode = pm;
#endif
            return newBlueprint;
        }

        public static T AddComponent<T>(GameObject owner, int blueprintId) where T : MonoBehaviour, IBlueprintableObject
        {
            var component = CreateBlueprintBehaviour<T>(owner, blueprintId);
#if UNITY_EDITOR
            var graph = RuntimeDatabase<BlueprintGraphSo>.Get(blueprintId);
            var pm = PlayModeClass.Create(graph);
            component.PlayMode = pm;
#endif
            return component;
        }
        
        private static T CreateBlueprintObject<T>(int blueprintId) where T : IBlueprintableObject, new()
        {
            var classType = RuntimeDataStore<BlueprintTypeContainer>.Get(blueprintId).Type;
            return (T)Activator.CreateInstance(classType);
        }

        private static T CreateBlueprintBehaviour<T>(GameObject owner, int blueprintId) where T : MonoBehaviour, IBlueprintableObject
        {
            var classType = RuntimeDataStore<BlueprintTypeContainer>.Get(blueprintId).GetType();
            return owner.AddComponent(classType) as T;
        }
    }
}
