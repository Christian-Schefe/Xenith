using ReactiveData.Core;
using System;

namespace ReactiveData.App
{
    public class ReactiveTempoEvent : IKeyed
    {
        public Reactive<float> beat;
        public Reactive<float> bps;

        public ReactiveTempoEvent(float beat, float bps)
        {
            this.beat = new(beat);
            this.bps = new(bps);
        }

        public string ID { get; private set; } = Guid.NewGuid().ToString();
        public string Key => ID;
    }
}
