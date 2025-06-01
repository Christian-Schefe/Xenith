using ReactiveData.Core;
using System;

namespace ReactiveData.App
{
    public class ReactiveTab : IKeyed
    {
        public Reactive<string> name;

        public ReactiveTab(string name)
        {
            this.name = new(name);
        }

        public string ID { get; private set; } = Guid.NewGuid().ToString();
        public string Key => ID;
    }
}
