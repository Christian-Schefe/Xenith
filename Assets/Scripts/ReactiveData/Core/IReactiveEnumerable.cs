using System.Collections.Generic;

namespace ReactiveData.Core
{
    public interface IReactiveEnumerable<T> : IEnumerable<T>, System.Collections.IEnumerable
    {
        event System.Action OnChanged;
    }
}