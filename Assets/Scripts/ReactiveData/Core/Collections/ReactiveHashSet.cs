using System;
using System.Collections;
using System.Collections.Generic;

namespace ReactiveData.Core
{
    public class ReactiveHashSet<T> : IReactiveEnumerable<T>, ISet<T>, ICollection<T>
    {
        private readonly HashSet<T> set;

        public int Count => set.Count;

        public bool IsReadOnly => ((ICollection<T>)set).IsReadOnly;

        public event Action OnChanged;

        public ReactiveHashSet()
        {
            set = new();
        }

        public ReactiveHashSet(IEnumerable<T> collection)
        {
            set = new(collection);
        }

        public bool Add(T item)
        {
            var result = set.Add(item);
            if (result) OnChanged?.Invoke();
            return result;
        }

        public void AddRange(IEnumerable<T> items)
        {
            bool changed = false;
            foreach (var item in items)
            {
                if (set.Add(item))
                {
                    changed = true;
                }
            }
            if (changed) OnChanged?.Invoke();
        }

        public void ExceptWith(IEnumerable<T> other)
        {
            set.ExceptWith(other);
            OnChanged?.Invoke();
        }

        public void IntersectWith(IEnumerable<T> other)
        {
            set.IntersectWith(other);
            OnChanged?.Invoke();
        }

        public bool IsProperSubsetOf(IEnumerable<T> other)
        {
            return set.IsProperSubsetOf(other);
        }

        public bool IsProperSupersetOf(IEnumerable<T> other)
        {
            return set.IsProperSupersetOf(other);
        }

        public bool IsSubsetOf(IEnumerable<T> other)
        {
            return set.IsSubsetOf(other);
        }

        public bool IsSupersetOf(IEnumerable<T> other)
        {
            return set.IsSupersetOf(other);
        }

        public bool Overlaps(IEnumerable<T> other)
        {
            return set.Overlaps(other);
        }

        public bool SetEquals(IEnumerable<T> other)
        {
            return set.SetEquals(other);
        }

        public void SymmetricExceptWith(IEnumerable<T> other)
        {
            set.SymmetricExceptWith(other);
            OnChanged?.Invoke();
        }

        public void UnionWith(IEnumerable<T> other)
        {
            set.UnionWith(other);
            OnChanged?.Invoke();
        }

        void ICollection<T>.Add(T item)
        {
            var result = set.Add(item);
            if (result) OnChanged?.Invoke();
        }

        public void Clear()
        {
            set.Clear();
            OnChanged?.Invoke();
        }

        public bool Contains(T item)
        {
            return set.Contains(item);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            set.CopyTo(array, arrayIndex);
        }

        public bool Remove(T item)
        {
            var result = set.Remove(item);
            if (result) OnChanged?.Invoke();
            return result;
        }

        public IEnumerator<T> GetEnumerator()
        {
            return set.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return set.GetEnumerator();
        }
    }
}
