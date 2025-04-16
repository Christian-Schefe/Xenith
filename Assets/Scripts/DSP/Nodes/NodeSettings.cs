using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Yeast;

namespace DSP
{
    public class NodeSettings
    {
        public Dictionary<string, NodeSetting> settings = new();

        public NodeSettings(params NodeSetting[] settings)
        {
            foreach (var setting in settings)
            {
                this.settings[setting.name] = setting;
            }
        }

        public T Get<T>(string name) where T : NodeSetting
        {
            return (T)settings[name];
        }

        public string Serialize()
        {
            var list = settings.Values.Select(setting => (setting.Type, setting.ToJson())).ToList();
            return list.ToJson();
        }

        public void Deserialize(string json)
        {
            var list = json.FromJson<List<(SettingType, string)>>();
            foreach (var (type, val) in list)
            {
                NodeSetting setting = type switch
                {
                    SettingType.Float => val.FromJson<FloatSetting>(),
                    SettingType.Int => val.FromJson<IntSetting>(),
                    _ => throw new System.Exception($"Unknown setting type: {type}"),
                };
                settings[setting.name] = setting;
            }
        }
    }

    public enum SettingType
    {
        Float,
        Int,
        String
    }

    public abstract class NodeSetting
    {
        public string name;
        public abstract SettingType Type { get; }

        public NodeSetting(string name)
        {
            this.name = name;
        }
    }

    public class FloatSetting : NodeSetting
    {
        public float value;

        public FloatSetting(string name, float value) : base(name)
        {
            this.value = value;
        }

        public override SettingType Type => SettingType.Float;
    }

    public class IntSetting : NodeSetting
    {
        public int value;

        public IntSetting(string name, int value) : base(name)
        {
            this.value = value;
        }

        public override SettingType Type => SettingType.Int;
    }

    public class StringSetting : NodeSetting
    {
        public string value;

        public StringSetting(string name, string value) : base(name)
        {
            this.value = value;
        }

        public override SettingType Type => SettingType.String;
    }
}
