using System;
using UnityEngine;
using Vapor.Inspector;
using Vapor.Keys;

namespace Vapor.Blueprints
{
    public interface IBlueprintableObject
    {
#if UNITY_EDITOR
        PlayModeClass PlayMode { get; set; }
#endif
    }

    [Blueprintable]
    public class BlueprintObject : IBlueprintableObject
    {
        
#if UNITY_EDITOR
        private int GraphKey => GetType().Name.GetStableHashU16();
        private PlayModeClass _playMode;
        public PlayModeClass PlayMode
        {
            get
            {
                if (_playMode != null)
                {
                    _playMode = PlayModeClass.Regenerate(_playMode);
                    return _playMode;
                }

                var graph = RuntimeDatabase<BlueprintGraphSo>.Get(GraphKey);
                var pm = PlayModeClass.Create(graph);
                _playMode = pm;
                return _playMode;
            }
            set => _playMode = value;
        }
#endif

        public virtual void CoolThingyMabob(int idx, out string nameee, out string nammeee2)
        {
            nameee = null;
            nammeee2 = null;
//             
// #if UNITY_EDITOR
//             // do weaver here
//             var __editorResult = PlayMode.Invoke(nameof(CoolThingyMabob), null, out var outArgs);
//             nameee = (string)outArgs[0];
//             nammeee2 = (string)outArgs[1];
//             return;
// #endif
//
//             // do generated code here
        }

        
    }

    public class ChildThingy : BlueprintObject
    {
        public override void CoolThingyMabob(int idx, out string nameee, out string nammeee2)
        {
#if UNITY_EDITOR
            // do weaver here
            var __editorResult = PlayMode.Invoke(nameof(CoolThingyMabob), null, out var outArgs);
            nameee = (string)outArgs[0];
            nammeee2 = (string)outArgs[1];
            return;
#endif
            base.CoolThingyMabob(idx, out nameee, out nammeee2);
        }
    }

    [Blueprintable]
    public class BlueprintBehaviour : VaporBehaviour, IBlueprintableObject
    {
#if UNITY_EDITOR
        private int GraphKey => GetType().Name.GetStableHashU16();
        private PlayModeClass _playMode;
        public PlayModeClass PlayMode
        {
            get
            {
                if (_playMode != null)
                {
                    _playMode = PlayModeClass.Regenerate(_playMode);
                    return _playMode;
                }

                var graph = RuntimeDatabase<BlueprintGraphSo>.Get(GraphKey);
                var pm = PlayModeClass.Create(graph);
                _playMode = pm;
                return _playMode;
            }
            set => _playMode = value;
        }
#endif
    }
}
