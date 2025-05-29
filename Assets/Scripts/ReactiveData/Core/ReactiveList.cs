using System;
using System.Collections.Generic;

namespace ReactiveData.Core
{
    public class ReactiveList<T> : List<T>
    {
        public event Action OnChanged;

        public ReactiveList() : base() { }
        public ReactiveList(IEnumerable<T> collection) : base(collection) { }

        public new void Add(T item)
        {
            base.Add(item);
            OnChanged?.Invoke();
        }

        public new void AddRange(IEnumerable<T> collection)
        {
            base.AddRange(collection);
            OnChanged?.Invoke();
        }

        public new void Remove(T item)
        {
            base.Remove(item);
            OnChanged?.Invoke();
        }

        public new void Clear()
        {
            base.Clear();
            OnChanged?.Invoke();
        }
    }
}
