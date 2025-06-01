using System;
using System.Collections.Generic;

namespace ReactiveData.Core
{
    public interface IReactive<T>
    {
        public T Value { get; }
        public event Action<T> OnChanged;
    }

    public class DerivedReactive<K, T> : IReactive<T>
    {
        private IReactive<K> source;
        private Func<K, T> transform;

        public event Action<T> OnChanged;

        public T Value => transform(source.Value);

        public DerivedReactive(IReactive<K> source, Func<K, T> transform)
        {
            this.source = source;
            this.transform = transform;
            source.OnChanged += Trigger;
        }

        public void Dispose()
        {
            if (source != null)
            {
                source.OnChanged -= Trigger;
                source = null;
                transform = null;
            }
        }

        private void Trigger(K value)
        {
            OnChanged?.Invoke(transform(value));
        }

        public void AddAndCall(Action<T> action)
        {
            OnChanged += action;
            action(Value);
        }
    }

    public class Reactive<T> : IReactive<T>
    {
        private T value;
        private IEqualityComparer<T> Comparer { get; }
        public event Action<T> OnChanged;

        public T Value
        {
            get => value;
            set
            {
                if (!Comparer.Equals(this.value, value))
                {
                    this.value = value;
                    OnChanged?.Invoke(this.value);
                }
            }
        }

        public Reactive(T initialValue = default, IEqualityComparer<T> comparer = null)
        {
            value = initialValue;
            Comparer = comparer ?? EqualityComparer<T>.Default;
        }

        public void AddAndCall(Action<T> action)
        {
            OnChanged += action;
            action(value);
        }

        public void Add(Action<T> action)
        {
            OnChanged += action;
        }

        public void Remove(Action<T> action)
        {
            OnChanged -= action;
        }
    }
}
