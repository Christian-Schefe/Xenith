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

        public abstract void OnSettingsChanged();
    }
}
