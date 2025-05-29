using System;
using System.Collections.Generic;

namespace ReactiveData.Core
{
    public class Reactive<T>
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
