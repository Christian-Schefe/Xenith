using ReactiveData.Core;
using System;

namespace ReactiveData.App
{
    public class ReactiveConnection : IKeyed
    {
        public Reactive<ReactiveNode> fromNode, toNode;
        public Reactive<int> fromIndex, toIndex;

        public ReactiveConnection(ReactiveNode fromNode, ReactiveNode toNode, int fromIndex, int toIndex)
        {
            this.fromNode = new(fromNode);
            this.toNode = new(toNode);
            this.fromIndex = new(fromIndex);
            this.toIndex = new(toIndex);
        }

        public string ID { get; private set; } = Guid.NewGuid().ToString();
        public string Key => ID;
    }
}
