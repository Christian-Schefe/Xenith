using System;
using System.Collections.Generic;

namespace ReactiveData.Core
{
    public class ReactiveHashSet<T> : HashSet<T>
    {
        public event Action OnChanged;

        public ReactiveHashSet() : base() { }
        public ReactiveHashSet(IEnumerable<T> collection) : base(collection) { }

        public new bool Add(T item)
        {
            var res = base.Add(item);
            OnChanged?.Invoke();
            return res;
        }

        public void AddRange(IEnumerable<T> collection)
        {
            foreach (var item in collection) base.Add(item);
            OnChanged?.Invoke();
        }

        public new bool Remove(T item)
        {
            var res = base.Remove(item);
            OnChanged?.Invoke();
            return res;
        }

        public new void Clear()
        {
            base.Clear();
            OnChanged?.Invoke();
        }
    }
}
