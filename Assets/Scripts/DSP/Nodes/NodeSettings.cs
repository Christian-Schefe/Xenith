using System;
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
            var list = settings.Values.Select(setting => (setting.name, setting.Serialize())).ToList();
            return list.ToJson();
        }

        public void Deserialize(string json)
        {
            var list = json.FromJson<List<(string, string)>>();
            foreach (var (name, val) in list)
            {
                settings[name].Deserialize(val);
            }
        }
    }

    public enum SettingType
    {
        Float,
        Int,
        String,
        Enum
    }

    public abstract class NodeSetting
    {
        public string name;
        public abstract SettingType Type { get; }

        public abstract string Serialize();
        public abstract void Deserialize(string str);

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

        public override string Serialize() => value.ToString("R");
        public override void Deserialize(string str) => value = float.TryParse(str, out var result) ? result : 0f;
    }

    public class IntSetting : NodeSetting
    {
        public int value;

        public IntSetting(string name, int value) : base(name)
        {
            this.value = value;
        }

        public override SettingType Type => SettingType.Int;

        public override string Serialize() => value.ToString();
        public override void Deserialize(string str) => value = int.TryParse(str, out var result) ? result : 0;
    }

    public class StringSetting : NodeSetting
    {
        public string value;

        public StringSetting(string name, string value) : base(name)
        {
            this.value = value;
        }

        public override SettingType Type => SettingType.String;

        public override string Serialize() => value;
        public override void Deserialize(string str) => value = str;
    }

    public class EnumSetting : NodeSetting
    {
        public Type type;
        public int value;

        public EnumSetting(string name, Type type, int value) : base(name)
        {
            this.type = type;
            this.value = value;
        }

        public override SettingType Type => SettingType.Enum;

        public override string Serialize() => value.ToString();
        public override void Deserialize(string str) => value = int.TryParse(str, out var result) ? result : 0;
    }

    public class EnumSetting<T> : EnumSetting where T : struct, IConvertible
    {
        public EnumSetting(string name, T value) : base(name, typeof(T), 0)
        {
            if (!typeof(T).IsEnum)
            {
                throw new ArgumentException("T must be of type Enum");
            }
            if (Enum.GetUnderlyingType(typeof(T)) != typeof(int))
            {
                throw new ArgumentException("Enum must have int as underlying type.");
            }
            this.value = Convert.ToInt32(value);
        }
    }
}
