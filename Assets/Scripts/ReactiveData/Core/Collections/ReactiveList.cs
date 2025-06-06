using System;
using System.Collections;
using System.Collections.Generic;

namespace ReactiveData.Core
{
    public class ReactiveList<T> : IReactiveEnumerable<T>, IList<T>, ICollection<T>
    {
        private readonly List<T> list;

        public int Count => list.Count;

        public bool IsReadOnly => ((ICollection<T>)list).IsReadOnly;

        public T this[int index]
        {
            get => list[index]; set
            {
                list[index] = value;
                OnChanged?.Invoke();
            }
        }

        public event Action OnChanged;

        public ReactiveList()
        {
            list = new();
        }
        public ReactiveList(IEnumerable<T> collection)
        {
            list = new(collection);
        }

        public int IndexOf(T item)
        {
            return list.IndexOf(item);
        }

        public void Insert(int index, T item)
        {
            list.Insert(index, item);
            OnChanged?.Invoke();
        }

        public void RemoveAt(int index)
        {
            list.RemoveAt(index);
            OnChanged?.Invoke();
        }

        public void Add(T item)
        {
            list.Add(item);
            OnChanged?.Invoke();
        }

        public void Clear()
        {
            list.Clear();
            OnChanged?.Invoke();
        }

        public void ReplaceAll(IEnumerable<T> collection)
        {
            list.Clear();
            list.AddRange(collection);
            OnChanged?.Invoke();
        }

        public bool Contains(T item)
        {
            return list.Contains(item);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            list.CopyTo(array, arrayIndex);
        }

        public bool Remove(T item)
        {
            var result = list.Remove(item);
            if (result) OnChanged?.Invoke();
            return result;
        }

        public IEnumerator<T> GetEnumerator()
        {
            return list.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return list.GetEnumerator();
        }

        public void Sort(Comparison<T> comparer)
        {
            list.Sort(comparer);
            OnChanged?.Invoke();
        }
    }
}
