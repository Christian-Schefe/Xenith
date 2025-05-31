using System.Collections.Generic;
using System;

namespace ReactiveData.Core
{
    public class ReactiveListBinder<TData, TUI> where TData : IKeyed where TUI : IReactor<TData>
    {
        private readonly Dictionary<string, TUI> uiElements = new();

        private IReactiveEnumerable<TData> source;
        private Func<TData, TUI> createFunc;
        private Action<TUI> destroyFunc;

        public ReactiveListBinder(IReactiveEnumerable<TData> source, Func<TData, TUI> createFunc, Action<TUI> destroyFunc)
        {
            this.source = source;
            this.createFunc = createFunc;
            this.destroyFunc = destroyFunc;

            source.OnChanged += OnListChanged;
            OnListChanged();
        }

        public IEnumerable<TUI> UIElements => uiElements.Values;

        public void Dispose()
        {
            source.OnChanged -= OnListChanged;

            foreach (var ui in uiElements.Values)
            {
                ui.Unbind();
                destroyFunc(ui);
            }
            uiElements.Clear();
            source = null;
            createFunc = null;
            destroyFunc = null;
        }

        private void OnListChanged()
        {
            var newKeys = new HashSet<string>();
            foreach (var item in source)
            {
                newKeys.Add(item.Key);
                if (!uiElements.TryGetValue(item.Key, out var ui))
                {
                    var instance = createFunc(item);
                    uiElements[item.Key] = instance;
                    instance.Bind(item);
                }
            }

            var toRemove = new List<string>();
            foreach (var key in uiElements.Keys)
            {
                if (!newKeys.Contains(key))
                    toRemove.Add(key);
            }

            foreach (var key in toRemove)
            {
                destroyFunc(uiElements[key]);
                uiElements[key].Unbind();
                uiElements.Remove(key);
            }
        }
    }
}
