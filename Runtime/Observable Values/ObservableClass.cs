using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Vapor.Observables
{
    /// <summary>
    /// Container for a collection of saved fields.
    /// </summary>
    [Serializable]
    public struct SavedObservableClass
    {
        public string Name;
        public string ClassType;
        public SavedObservable[] SavedFields;

        public SavedObservableClass(string name, Type type, List<SavedObservable> fields)
        {
            Name = name;
            ClassType = type.AssemblyQualifiedName;
            SavedFields = fields.ToArray();
        }
    }

    public interface IObservedClass
    {
        void SetupFields(ObservableClass @class)
        {

        }
    }

    /// <summary>
    /// An abstract implementation of an observable class that can keep track of a collection of Observables.
    /// When a value is changed inside the monitored collection the entire class will be marked dirty.
    /// This class also facillitates serializing and deserializing the class into a json format.
    /// The one requirement of this class is it must implement a constructor that only implements the default string named arguments.
    /// <code>
    /// public class ChildObservableClass
    /// {
    ///     public ChildObservableClass(string className) : base(className) { }
    ///     
    ///     protected override SetupFields()
    ///     {
    ///         // Field initialization should be done here, not in the constructor.
    ///     }
    /// }
    /// </code>
    /// </summary>
    public abstract class ObservableClass
    {
        /// <summary>
        /// A unique name for this instance of the class.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// A unique id for this instance of the class.
        /// </summary>
        public ushort Id { get; set; }

        /// <summary>
        /// Gets a field based on the ID and casts it to a type that inherits from <see cref="ObservableField"/>. There is no checking, will throw errors on invalid id.
        /// </summary>
        /// <param name="fieldID">The id of the field to retrieve</param>
        /// <typeparam name="T">The type to cast the field to</typeparam>
        /// <returns>The <see cref="ObservableField"/> of type T</returns>
        public T GetField<T>(string fieldName) where T : Observable => (T)Fields[fieldName.GetStableHashU16()];
        public T GetField<T>(ushort fieldId) where T : Observable => (T)Fields[fieldId];

        public T GetFieldValue<T>(string fieldName) where T : struct => GetField<Observable<T>>(fieldName).Value;
        public T GetFieldValue<T>(ushort fieldId) where T : struct => GetField<Observable<T>>(fieldId).Value;

        public void SetFieldValue<T>(string fieldName, T value) where T : struct => GetField<Observable<T>>(fieldName).Value = value;
        public void SetFieldValue<T>(ushort fieldId, T value) where T : struct => GetField<Observable<T>>(fieldId).Value = value;

        protected readonly Dictionary<ushort, Observable> Fields = new();
        protected bool IsLoaded = false;

        /// <summary>
        /// This event is fired when the <see cref="ObservableField"/>s of the class change.
        /// </summary>
        public event Action<ObservableClass, Observable> Dirtied;

        protected ObservableClass(ushort id)
        {
            Name = id.ToString();
            Id = id;
            SetupFields();
        }

        protected ObservableClass(string className)
        {
            Name = className;
            Id = Name.GetStableHashU16();
            SetupFields();
        }

        #region - Fields -
        /// <summary>
        /// This method should add all the default fields to the derived class.
        /// </summary>
        protected abstract void SetupFields();

        public Observable<T> GetOrAddField<T>(ushort fieldId, bool saveValue, T value, Action<Observable<T>, T> callback = null) where T : struct
        {
            if (!Fields.ContainsKey(fieldId))
            {
                return AddField(fieldId, saveValue, value, callback);
            }
            else
            {
                var field = (Observable<T>)Fields[fieldId];
                field.WithChanged(callback);
                return field;
            }
        }

        public Observable<T> GetOrAddField<T>(string fieldName, bool saveValue, T value, Action<Observable<T>, T> callback = null) where T : struct
        {
            var id = fieldName.GetStableHashU16();
            if (!Fields.ContainsKey(id))
            {
                return AddField(fieldName, saveValue, value, callback);
            }
            else
            {
                var field = (Observable<T>)Fields[id];
                field.WithChanged(callback);
                return field;
            }
        }

        public Observable<T> AddField<T>(ushort fieldId, bool saveValue, T value, Action<Observable<T>, T> callback = null) where T : struct
        {
            if (!Fields.ContainsKey(fieldId))
            {
                var field = new Observable<T>(fieldId, saveValue, value).WithChanged(callback);
                field.WithDirtied(MarkDirty);
                Fields.Add(fieldId, field);
                MarkDirty(Fields[fieldId]);
                return field;
            }
            else
            {
                Debug.LogError($"Field [{fieldId}] already added to class {Name}");
                return (Observable<T>)Fields[fieldId];
            }
        }

        public Observable<T> AddField<T>(string fieldName, bool saveValue, T value, Action<Observable<T>, T> callback = null) where T : struct
        {
            var id = fieldName.GetStableHashU16();
            if (!Fields.ContainsKey(id))
            {
                var field = new Observable<T>(fieldName, saveValue, value).WithChanged(callback);
                field.WithDirtied(MarkDirty);
                Fields.Add(id, field);
                MarkDirty(Fields[id]);
                return field;
            }
            else
            {
                Debug.LogError($"Field [{fieldName}] already added to class {Name}");
                return (Observable<T>)Fields[id];
            }
        }

        public void AddField(Observable field)
        {
            if (Fields.TryAdd(field.Id, field))
            {
                field.WithDirtied(MarkDirty);
                MarkDirty(field);
            }
            else
            {
                Debug.LogError($"Field [{field.Name}] already added to class {Name}");
            }
        }

        public void RemoveField(string fieldName)
        {
            RemoveField(fieldName.GetStableHashU16());
        }
        public void RemoveField(ushort fieldId)
        {
            if (Fields.TryGetValue(fieldId, out var field))
            {
                field.ClearCallbacks();
                Fields.Remove(fieldId);
            }
        }

        internal virtual void MarkDirty(Observable field)
        {
            Dirtied?.Invoke(this, field);
        }
        #endregion

        #region - Saving and Loading -
        public string SaveAsJson()
        {
            var save = Save();
            return JsonConvert.SerializeObject(save, ObservableSerializerUtility.s_JsonSerializerSettings);
        }

        public SavedObservableClass Save()
        {
            List<SavedObservable> holder = new();
            foreach (var field in Fields.Values)
            {
                if (field.SaveValue)
                {
                    holder.Add(field.Save());
                }
            }
            return new SavedObservableClass(Name, GetType(), holder);
        }

        public static SavedObservableClass Load(string json)
        {
            return JsonConvert.DeserializeObject<SavedObservableClass>(json, ObservableSerializerUtility.s_JsonSerializerSettings);
        }

        public void Load(SavedObservableClass load)
        {
            if (!IsLoaded)
            {
                //var valueType = Type.GetType(load.ClassType);
                //var result = Activator.CreateInstance(valueType, new object[] { load.Name }) as ObservableClass;
                if (load.SavedFields != null)
                {
                    foreach (var field in load.SavedFields)
                    {
                        var obs = Observable.Load(field);
                        var id = field.Name.GetStableHashU16();
                        if (Fields.ContainsKey(id))
                        {
                            Fields[id].SetValueBoxed(obs.GetValueBoxed());
                        }
                        else
                        {
                            AddField(obs);
                        }
                    }
                }
                IsLoaded = true;
            }
        }
        #endregion
    }

    public class ObservableClass<T> : ObservableClass where T : IObservedClass
    {
        public T ObservedClass { get; }

        public ObservableClass(string className, T observedClass) : base(className)
        {
            ObservedClass = observedClass;
            ObservedClass.SetupFields(this);
        }

        protected override void SetupFields()
        {

        }
    }
}
