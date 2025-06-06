using System.Collections.Generic;
using System;
using System.Collections;


namespace ReactiveData.Core
{
    public class ReactiveDerivedEnumerable<TFrom, TTo> : IReactiveEnumerable<TTo>
    {
        private readonly IReactiveEnumerable<TFrom> source;
        private readonly Func<TFrom, TTo> deriver;

        public event Action OnChanged;

        public ReactiveDerivedEnumerable(IReactiveEnumerable<TFrom> source, Func<TFrom, TTo> deriver)
        {
            this.source = source;
            this.deriver = deriver;
            this.source.OnChanged += OnChanged;
        }

        public IEnumerator<TTo> GetEnumerator()
        {
            foreach (var item in source)
            {
                yield return deriver(item);
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
