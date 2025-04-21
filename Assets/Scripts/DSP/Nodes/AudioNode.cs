using System;
using System.Collections.Generic;
using Yeast;

namespace DSP
{
    public abstract class AudioNode
    {
        public List<NamedValue> inputs;
        public List<NamedValue> outputs;

        public abstract List<NamedValue> BuildInputs();
        public abstract List<NamedValue> BuildOutputs();

        public virtual void Initialize()
        {
            inputs = BuildInputs();
            outputs = BuildOutputs();
        }

        public abstract void Process(Context context);
        public abstract void ResetState();
        public abstract AudioNode Clone();
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
        protected NodeSettings settings;
        public NodeSettings Settings => settings;
        public abstract NodeSettings DefaultSettings { get; }

        public SettingsNode()
        {
            settings = DefaultSettings;
            OnSettingsChanged();
        }

        public void ApplySettings(NodeSettings newSettings)
        {
            settings = newSettings;
            OnSettingsChanged();
        }

        public void DeserializeSettings(string serializedSettings)
        {
            settings.Deserialize(serializedSettings);
            OnSettingsChanged();
        }

        public abstract void OnSettingsChanged();
        protected abstract SettingsNode CloneWithoutSettings();
        public override AudioNode Clone()
        {
            var clone = CloneWithoutSettings();
            settings.CloneInto(clone.settings);
            clone.OnSettingsChanged();
            return clone;
        }
    }
}
