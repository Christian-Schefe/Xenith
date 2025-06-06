using ReactiveData.Core;
using System;

namespace ReactiveData.App
{
    public class ReactiveNote : IKeyed
    {
        public Reactive<float> beat;
        public Reactive<int> pitch;
        public Reactive<float> velocity;
        public Reactive<float> length;

        public ReactiveNote(float beat, int pitch, float velocity, float length)
        {
            this.beat = new(beat);
            this.pitch = new(pitch);
            this.velocity = new(velocity);
            this.length = new(length);
        }

        public string ID { get; private set; } = Guid.NewGuid().ToString();
        public string Key => ID;
    }
}
