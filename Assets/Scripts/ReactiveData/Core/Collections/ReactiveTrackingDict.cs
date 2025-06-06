using System.Collections.Generic;
using System;
using System.Collections;

namespace ReactiveData.Core
{
    public class ReactiveTrackingDict<TFrom, TTo> : IReactiveEnumerable<TTo>, IReadOnlyDictionary<TFrom, TTo> where TFrom : IKeyed
    {
        private readonly Dictionary<string, TTo> derivedCache;
        private readonly IReactiveEnumerable<TFrom> source;
        private readonly Func<TFrom, TTo> createFunc;
        private readonly Action<TTo> destroyFunc;

        public ReactiveTrackingDict(IReactiveEnumerable<TFrom> source, Func<TFrom, TTo> createFunc, Action<TTo> destroyFunc = null)
        {
            derivedCache = new();
            this.source = source;
            this.createFunc = createFunc;
            this.destroyFunc = destroyFunc;

            source.OnChanged += OnSourceChanged;
            OnSourceChanged();
        }

        public TTo this[TFrom key] => derivedCache[key.Key];

        public IEnumerable<TTo> UIElements => derivedCache.Values;

        public IEnumerable<TFrom> Keys => source;

        public IEnumerable<TTo> Values => derivedCache.Values;

        public int Count => derivedCache.Count;


        public event Action OnChanged;

        public bool ContainsKey(TFrom key)
        {
            return derivedCache.ContainsKey(key.Key);
        }

        public IEnumerator<TTo> GetEnumerator()
        {
            foreach (var item in derivedCache.Values)
            {
                yield return item;
            }
        }

        public bool TryGetValue(TFrom key, out TTo value)
        {
            if (derivedCache.TryGetValue(key.Key, out value))
            {
                return true;
            }
            value = default;
            return false;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        IEnumerator<KeyValuePair<TFrom, TTo>> IEnumerable<KeyValuePair<TFrom, TTo>>.GetEnumerator()
        {
            foreach (var item in source)
            {
                yield return new KeyValuePair<TFrom, TTo>(item, derivedCache[item.Key]);
            }
        }

        private void OnSourceChanged()
        {
            var newKeys = new HashSet<string>();
            foreach (var item in source)
            {
                newKeys.Add(item.Key);
                if (!derivedCache.TryGetValue(item.Key, out var ui))
                {
                    var instance = createFunc(item);
                    derivedCache[item.Key] = instance;
                }
            }

            var toRemove = new List<string>();
            foreach (var key in derivedCache.Keys)
            {
                if (!newKeys.Contains(key))
                    toRemove.Add(key);
            }

            foreach (var key in toRemove)
            {
                destroyFunc?.Invoke(derivedCache[key]);
                derivedCache.Remove(key);
            }
            OnChanged?.Invoke();
        }
    }
}
