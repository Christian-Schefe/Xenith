using System;
using System.Collections.Generic;
using Unity.Collections.LowLevel.Unsafe;

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

    public interface IWritableReactive<T> : IReactive<T>
    {
        public void SetValue(T value);
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

        public void AddAndCall(Action<T> action)
        {
            OnChanged += action;
            action(Value);
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

    public abstract class WritableReactiveBase<T> : IWritableReactive<T>, IReactive
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

        public void SetValue(T value) => Value = value;

        public void AddAndCall(Action<T> action)
        {
            OnChanged += action;
            action(Value);
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

    public class TwoWayDerivedReactive<K, T> : DerivedReactive<K, T>, IWritableReactive<T>
    {
        private IWritableReactive<K> source;
        private Func<T, K> backTransform;

        public TwoWayDerivedReactive(IWritableReactive<K> source, Func<K, T> transform, Func<T, K> backTransform, IEqualityComparer<T> comparer = null) : base(source, transform, comparer)
        {
            this.source = source;
            this.backTransform = backTransform;
        }

        public override void Dispose()
        {
            base.Dispose();
            backTransform = null;
            source = null;
        }

        public void SetValue(T value)
        {
            source.SetValue(backTransform(value));
        }
    }

    public class DerivedReactive<K, T> : ReactiveBase<T>
    {
        private IReactive<K> source;
        private Func<K, T> transform;

        private T value;

        public override T Value => value;
        private IEqualityComparer<T> Comparer { get; }

        public DerivedReactive(IReactive<K> source, Func<K, T> transform, IEqualityComparer<T> comparer = null)
        {
            this.source = source;
            this.transform = transform;
            source.OnChanged += Trigger;
            value = transform(source.Value);
            Comparer = comparer ?? EqualityComparer<T>.Default;
        }

        public virtual void Dispose()
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
            var newValue = transform(value);
            if (!Comparer.Equals(this.value, newValue))
            {
                this.value = newValue;
                MarkChange();
            }
        }
    }

    public class NestedReactive<K, T> : ReactiveBase<T>
    {
        private IReactive<K> source;
        private Func<K, IReactive<T>> keySelector;

        private IReactive<T> key;

        public override T Value => key == null ? default : key.Value;

        public NestedReactive(IReactive<K> source, Func<K, IReactive<T>> keySelector)
        {
            this.source = source;
            this.keySelector = keySelector;
            source.OnChanged += TriggerSource;
            key = keySelector(source.Value);
            if (key != null) key.OnChanged += TriggerKey;
        }

        public void Dispose()
        {
            if (source != null)
            {
                source.OnChanged -= TriggerSource;
                source = null;
            }
            keySelector = null;
        }

        private void TriggerSource(K value)
        {
            if (key != null) key.OnChanged -= TriggerKey;
            key = keySelector(value);
            if (key != null) key.OnChanged += TriggerKey;
            MarkChange();
        }

        private void TriggerKey(T value)
        {
            MarkChange();
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
    }
}
