using System;
using System.Collections.Generic;
using UnityEngine;

namespace Vapor.Observables
{
    [Serializable]
    public class ObservableList<T>
    {
        public List<T> Items;

        public T this[int index] => Items[index];
        public int Count => Items.Count;

        public event Action<List<T>> ListChanged = delegate { };

        public ObservableList()
        {
            Items = new List<T>();
        }

        public ObservableList(int capacity)
        {
            Items = new List<T>(capacity);
        }

        public ObservableList(IList<T> baseList)
        {
            Items = new List<T>(baseList.Count);
            Items.AddRange(baseList);
        }

        private void Invoke() => ListChanged.Invoke(Items);

        public void Swap(int index1, int index2)
        {
            (Items[index1], Items[index2]) = (Items[index2], Items[index1]);
            Invoke();
        }

        public void Clear()
        {
            Items.Clear();
            Invoke();
        }

        public void Add(T item)
        {
            Items.Add(item);
            Invoke();
        }

        public void Insert(int index, T item)
        {
            Items.Insert(index, item);
            Invoke();
        }

        public bool Remove(T item)
        {
            var removed = Items.Remove(item);
            if (removed)
                Invoke();
            return removed;
        }

        public void RemoveAt(int index)
        {
            Items.RemoveAt(index);
            Invoke();
        }
    }
}
