using System.Collections.Generic;
using System;
using System.Collections;


namespace ReactiveData.Core
{
    public class ReactiveChainedEnumerable<T> : IReactiveEnumerable<T>
    {
        private readonly List<IReactiveEnumerable<T>> sources;

        public event Action OnChanged;

        public ReactiveChainedEnumerable()
        {
            sources = new();
        }

        public ReactiveChainedEnumerable(IReactiveEnumerable<T> source)
        {
            sources = new() { source };
            source.OnChanged += Trigger;
        }

        public ReactiveChainedEnumerable(IEnumerable<IReactiveEnumerable<T>> sources)
        {
            this.sources = new List<IReactiveEnumerable<T>>(sources);
            foreach (var source in sources)
            {
                source.OnChanged += Trigger;
            }
        }

        private void Trigger()
        {
            OnChanged?.Invoke();
        }

        public void AddSource(IReactiveEnumerable<T> source)
        {
            sources.Add(source);
            source.OnChanged += Trigger;
            OnChanged?.Invoke();
        }

        public void AddSources(IEnumerable<IReactiveEnumerable<T>> sources)
        {
            this.sources.AddRange(sources);
            foreach (var source in sources)
            {
                source.OnChanged += Trigger;
            }
            OnChanged?.Invoke();
        }

        public void RemoveSource(IReactiveEnumerable<T> source)
        {
            if (sources.Remove(source))
            {
                source.OnChanged -= Trigger;
            }
            OnChanged?.Invoke();
        }

        public void ReplaceSources(IEnumerable<IReactiveEnumerable<T>> newSources)
        {
            foreach (var source in sources)
            {
                source.OnChanged -= Trigger;
            }
            sources.Clear();
            AddSources(newSources);
        }

        public void ClearSources()
        {
            foreach (var source in sources)
            {
                source.OnChanged -= Trigger;
            }
            sources.Clear();
            OnChanged?.Invoke();
        }

        public IEnumerator<T> GetEnumerator()
        {
            if (sources.Count == 0)
            {
                yield break;
            }

            foreach (var source in sources)
            {
                foreach (var item in source)
                {
                    yield return item;
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
