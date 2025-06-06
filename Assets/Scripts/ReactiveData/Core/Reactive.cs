using System;
using System.Collections.Generic;

namespace ReactiveData.Core
{
    public interface IReactive
    {
        public event Action OnChanged;
    }

    public interface IReactive<T>
    {
        public T Value { get; }
        public event Action<T> OnChanged;
    }

    public abstract class ReactiveBase<T> : IReactive<T>, IReactive
    {
        public abstract T Value { get; }

        public event Action<T> OnChanged;
        private Action onChanged;


        event Action IReactive.OnChanged
        {
            add => onChanged += value;
            remove => onChanged -= value;
        }

        public ReactiveBase()
        {
            OnChanged += value => onChanged?.Invoke();
        }

        protected void MarkChange()
        {
            OnChanged?.Invoke(Value);
        }
    }

    public abstract class WritableReactiveBase<T> : IReactive<T>, IReactive
    {
        public abstract T Value { get; set; }

        public event Action<T> OnChanged;
        private Action onChanged;


        event Action IReactive.OnChanged
        {
            add => onChanged += value;
            remove => onChanged -= value;
        }

        public WritableReactiveBase()
        {
            OnChanged += value => onChanged?.Invoke();
        }

        protected void MarkChange()
        {
            OnChanged?.Invoke(Value);
        }
    }

    public class DerivedReactive<T> : DerivedReactive<T, T>
    {
        public DerivedReactive(IReactive<T> source, Func<T, T> transform) : base(source, transform) { }
    }

    public class DerivedReactive<K, T> : ReactiveBase<T>
    {
        private IReactive<K> source;
        private Func<K, T> transform;

        public override T Value => transform(source.Value);

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
            }
            transform = null;
        }

        private void Trigger(K value)
        {
            MarkChange();
        }

        public void AddAndCall(Action<T> action)
        {
            OnChanged += action;
            action(Value);
        }
    }

    public class Reactive<T> : WritableReactiveBase<T>
    {
        private T value;
        private IEqualityComparer<T> Comparer { get; }

        public override T Value
        {
            get => value;
            set
            {
                if (!Comparer.Equals(this.value, value))
                {
                    this.value = value;
                    MarkChange();
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
