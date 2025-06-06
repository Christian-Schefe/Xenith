using System;
using System.Collections;
using System.Collections.Generic;

namespace ReactiveData.Core
{
    public class ReactiveDict<TKey, TValue> : IReactiveEnumerable<KeyValuePair<TKey, TValue>>, IDictionary<TKey, TValue>, ICollection<KeyValuePair<TKey, TValue>>
    {
        private readonly Dictionary<TKey, TValue> dict;
        private readonly IReactiveEnumerable<TKey> reactiveKeys;
        private readonly IReactiveEnumerable<TValue> reactiveValues;

        public IReactiveEnumerable<TKey> Keys => reactiveKeys;
        public IReactiveEnumerable<TValue> Values => reactiveValues;

        ICollection<TKey> IDictionary<TKey, TValue>.Keys => dict.Keys;
        ICollection<TValue> IDictionary<TKey, TValue>.Values => dict.Values;

        public int Count => dict.Count;

        public bool IsReadOnly => ((ICollection<KeyValuePair<TKey, TValue>>)dict).IsReadOnly;

        public TValue this[TKey key]
        {
            get => dict[key]; set
            {
                dict[key] = value;
                OnChanged?.Invoke();
            }
        }

        public event Action OnChanged;

        public ReactiveDict()
        {
            dict = new();
            reactiveKeys = new ReactiveDerivedEnumerable<KeyValuePair<TKey, TValue>, TKey>(this, pair => pair.Key);
            reactiveValues = new ReactiveDerivedEnumerable<KeyValuePair<TKey, TValue>, TValue>(this, pair => pair.Value);
        }

        public ReactiveDict(IEnumerable<KeyValuePair<TKey, TValue>> collection)
        {
            dict = new(collection);
            reactiveKeys = new ReactiveDerivedEnumerable<KeyValuePair<TKey, TValue>, TKey>(this, pair => pair.Key);
            reactiveValues = new ReactiveDerivedEnumerable<KeyValuePair<TKey, TValue>, TValue>(this, pair => pair.Value);
        }

        public void Add(TKey key, TValue value)
        {
            dict.Add(key, value);
            OnChanged?.Invoke();
        }

        public bool ContainsKey(TKey key)
        {
            return dict.ContainsKey(key);
        }

        public bool Remove(TKey key)
        {
            bool result = dict.Remove(key);
            if (result) OnChanged?.Invoke();
            return result;
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            return dict.TryGetValue(key, out value);
        }

        public void Add(KeyValuePair<TKey, TValue> item)
        {
            ((ICollection<KeyValuePair<TKey, TValue>>)dict).Add(item);
            OnChanged?.Invoke();
        }

        public void Clear()
        {
            dict.Clear();
            OnChanged?.Invoke();
        }

        public bool Contains(KeyValuePair<TKey, TValue> item)
        {
            return ((ICollection<KeyValuePair<TKey, TValue>>)dict).Contains(item);
        }

        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            ((ICollection<KeyValuePair<TKey, TValue>>)dict).CopyTo(array, arrayIndex);
        }

        public bool Remove(KeyValuePair<TKey, TValue> item)
        {
            var result = ((ICollection<KeyValuePair<TKey, TValue>>)dict).Remove(item);
            if (result) OnChanged?.Invoke();
            return result;
        }

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            return dict.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return dict.GetEnumerator();
        }
    }
}
