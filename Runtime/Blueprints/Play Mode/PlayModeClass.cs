using System;
using System.Collections.Generic;
using System.Linq;

namespace Vapor.Blueprints
{
    public class PlayModeClass : BlueprintObject
    {
        public static PlayModeClass Create(BlueprintGraphSo graphSo)
        {
            var dto = BlueprintClassGraphDto.Load(graphSo.GraphJson);
            return new PlayModeClass(graphSo, dto);
        }

        public static PlayModeClass Regenerate(PlayModeClass playModeClass)
        {
            return playModeClass.RequiresRegenerate ? Create(playModeClass.Graph) : playModeClass;
        }

        public BlueprintGraphSo Graph { get; }
        public bool RequiresRegenerate => Graph.Version != _version;
        
        private readonly Dictionary<string, object> _variables = new ();
        private readonly Dictionary<string, PlayModeMethod> _methods = new ();
        private readonly long _version;

        public PlayModeClass(BlueprintGraphSo graph, BlueprintClassGraphDto dto)
        {
            Graph = graph;
            _version = Graph.Version;
            foreach (var variable in dto.Variables)
            {
                if (variable.ConstructorName.Equals("Default(T)"))
                {
                    var defaultValue = TypeUtility.CastToType(variable.DefaultParametersValue[0].Item2, variable.DefaultParametersValue[0].Item1);
                    _variables[variable.Id] = defaultValue;
                }
                else
                {
                    var defaultValue = Activator.CreateInstance(variable.Type, variable.DefaultParametersValue.Select(t => TypeUtility.CastToType(t.Item2, t.Item1)));
                    _variables[variable.Id] = defaultValue;
                }
            }

            foreach (var method in dto.Methods)
            {
                var pm = new PlayModeMethod(this, method);
                _methods.Add(method.MethodName, pm);
            }
        }
        
        public object Invoke(string methodName, object[] parameters, out object[] outParameters)
        {
            if (_methods.TryGetValue(methodName, out var method))
            {
                return method.Invoke(parameters, out outParameters);
            }

            outParameters = null;
            return null;
        }
        
        public object GetVariable(string variableId)
        {
            return _variables[variableId];
        }
        
        public void SetVariable(string variableId, object value)
        {
            _variables[variableId] = value;
        }
    }
}
