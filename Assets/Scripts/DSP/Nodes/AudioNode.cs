using System.Collections.Generic;
using System.Linq;

namespace DSP
{
    public abstract class AudioNode
    {
        public List<NamedValue> inputs;
        public List<NamedValue> outputs;

        public abstract List<NamedValue> BuildInputs();
        public abstract List<NamedValue> BuildOutputs();

        public virtual void Initialize(Context context)
        {
            inputs = BuildInputs();
            outputs = BuildOutputs();
        }
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

        public override void Initialize(Context context)
        {
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
