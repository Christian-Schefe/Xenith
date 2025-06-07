using System;
using System.Collections.Generic;
using System.Linq;
using Yeast;

namespace DSP
{
    public abstract class AudioNode
    {
        public readonly List<NamedValue> inputs;
        public readonly List<NamedValue> outputs;

        public abstract List<NamedValue> BuildInputs();
        public abstract List<NamedValue> BuildOutputs();

        public AudioNode()
        {
            inputs = BuildInputs();
            outputs = BuildOutputs();
        }

        public virtual void Initialize() { }
        public abstract void Process(Context context);
        public abstract void ResetState();
        public abstract AudioNode Clone();
        public virtual void Dispose() { }
    }

    public class EmptyNode : AudioNode
    {
        public override List<NamedValue> BuildInputs() => new();

        public override List<NamedValue> BuildOutputs() => new();

        public override void Process(Context context) { }

        public override void ResetState() { }
        public override AudioNode Clone() => new EmptyNode();
    }

    public abstract class SettingsNode : AudioNode
    {
        protected Dictionary<string, NodeSetting> settings;
        public Dictionary<string, NodeSetting> Settings => settings;
        public abstract List<NodeSetting> DefaultSettings { get; }

        public SettingsNode()
        {
            settings = DefaultSettings.ToDictionary(e => e.name, e => e);
            OnSettingsChanged();
        }

        public abstract void OnSettingsChanged();
        protected abstract SettingsNode CloneWithoutSettings();
        public override AudioNode Clone()
        {
            var clone = CloneWithoutSettings();
            foreach (var setting in settings)
            {
                setting.Value.CloneInto(clone.settings[setting.Key]);
            }
            clone.OnSettingsChanged();
            return clone;
        }
    }
}
