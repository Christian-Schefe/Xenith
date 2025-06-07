using System;
using System.Collections.Generic;
using System.Linq;
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

        public void CloneInto(NodeSettings target)
        {
            foreach (var setting in settings)
            {
                setting.Value.CloneInto(target.settings[setting.Key]);
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

        public abstract void CloneInto(NodeSetting other);

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
        public override void CloneInto(NodeSetting other) => ((FloatSetting)other).value = value;
    }

    public class IntSetting : NodeSetting
    {
        public int value;

        public IntSetting(string name, int value) : base(name)
        {
            this.value = value;
        }

        public override SettingType Type => SettingType.Int;
        public override void CloneInto(NodeSetting other) => ((IntSetting)other).value = value;
    }

    public class StringSetting : NodeSetting
    {
        public string value;

        public StringSetting(string name, string value) : base(name)
        {
            this.value = value;
        }

        public override SettingType Type => SettingType.String;
        public override void CloneInto(NodeSetting other) => ((StringSetting)other).value = value;
    }

    public class EnumSetting : IntSetting
    {
        public Dictionary<int, string> names;

        public override SettingType Type => SettingType.Enum;

        public EnumSetting(string name, int value, Dictionary<int, string> names) : base(name, 0)
        {
            this.value = Convert.ToInt32(value);
            this.names = names;
        }
    }

    public class EnumSetting<T> : EnumSetting where T : struct, IConvertible
    {
        public EnumSetting(string name, T value) : base(name, Convert.ToInt32(value), GetNames()) { }

        public EnumSetting(string name, T value, Dictionary<T, string> nameOverrides = null) : this(name, value)
        {
            if (nameOverrides != null)
            {
                foreach (var (val, valName) in nameOverrides)
                {
                    int key = Convert.ToInt32(val);
                    if (names.ContainsKey(key))
                    {
                        names[key] = valName;
                    }
                }
            }
        }

        private static Dictionary<int, string> GetNames()
        {
            if (!typeof(T).IsEnum)
            {
                throw new ArgumentException("T must be of type Enum");
            }
            if (Enum.GetUnderlyingType(typeof(T)) != typeof(int))
            {
                throw new ArgumentException("Enum must have int as underlying type.");
            }
            var options = ((T[])Enum.GetValues(typeof(T))).Select(val => Convert.ToInt32(val));
            return options.Zip(Enum.GetNames(typeof(T)), (val, name) => new { val, name }).ToDictionary(x => x.val, x => x.name);
        }
    }
}
