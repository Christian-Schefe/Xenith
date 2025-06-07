using ReactiveData.Core;
using System;
using System.Collections.Generic;
using Yeast;

namespace ReactiveData.App
{
    public enum ReactiveSettingType
    {
        Int,
        Float,
        String,
        Enum
    }

    public abstract class ReactiveNodeSetting : IKeyed
    {
        public Reactive<string> name;
        public string ID { get; private set; } = Guid.NewGuid().ToString();
        public string Key => ID;
        public abstract IReactive Value { get; }
        public abstract ReactiveSettingType Type { get; }

        public abstract string Serialize();
        public abstract void Deserialize(string str);
        public abstract ReactiveNodeSetting Clone();

        public ReactiveNodeSetting(string name)
        {
            this.name = new(name);
        }

        public static ReactiveNodeSetting Deserialize(string name, ReactiveSettingType type, string str)
        {
            ReactiveNodeSetting instance = type switch
            {
                ReactiveSettingType.Int => new ReactiveIntSetting(name, 0),
                ReactiveSettingType.Float => new ReactiveFloatSetting(name, 0f),
                ReactiveSettingType.String => new ReactiveStringSetting(name, string.Empty),
                ReactiveSettingType.Enum => new ReactiveEnumSetting(name, new(), 0),
                _ => throw new ArgumentException($"Unknown setting type: {type}")
            };
            instance.Deserialize(str);
            return instance;
        }
    }

    public abstract class ReactiveValueSetting<T> : ReactiveNodeSetting
    {
        public Reactive<T> value;
        public override IReactive Value => value;

        public ReactiveValueSetting(string name, T value) : base(name)
        {
            this.value = new(value);
        }
    }

    public class ReactiveIntSetting : ReactiveValueSetting<int>
    {
        public ReactiveIntSetting(string name, int value) : base(name, value) { }
        public override ReactiveSettingType Type => ReactiveSettingType.Int;

        public override string Serialize() => value.Value.ToString();
        public override void Deserialize(string str) => value.Value = int.TryParse(str, out var result) ? result : 0;
        public override ReactiveNodeSetting Clone() => new ReactiveIntSetting(name.Value, value.Value);
    }

    public class ReactiveFloatSetting : ReactiveValueSetting<float>
    {
        public ReactiveFloatSetting(string name, float value) : base(name, value) { }
        public override ReactiveSettingType Type => ReactiveSettingType.Float;

        public override string Serialize() => value.Value.ToString("R");
        public override void Deserialize(string str) => value.Value = float.TryParse(str, out var result) ? result : 0f;
        public override ReactiveNodeSetting Clone() => new ReactiveFloatSetting(name.Value, value.Value);
    }

    public class ReactiveStringSetting : ReactiveValueSetting<string>
    {
        public ReactiveStringSetting(string name, string value) : base(name, value) { }
        public override ReactiveSettingType Type => ReactiveSettingType.String;

        public override string Serialize() => value.Value;
        public override void Deserialize(string str) => value.Value = str ?? string.Empty;
        public override ReactiveNodeSetting Clone() => new ReactiveStringSetting(name.Value, value.Value);
    }

    public class ReactiveEnumSetting : ReactiveValueSetting<int>
    {
        public Dictionary<int, string> options;

        public ReactiveEnumSetting(string name, Dictionary<int, string> options, int value) : base(name, value)
        {
            this.options = options;
        }

        public override ReactiveSettingType Type => ReactiveSettingType.Enum;

        public override string Serialize() => (options, value).ToJson();
        public override void Deserialize(string str)
        {
            var data = str.FromJson<(Dictionary<int, string> options, int value)>();
            options = data.options;
            value.Value = data.value;
        }
        public override ReactiveNodeSetting Clone() => new ReactiveEnumSetting(name.Value, new Dictionary<int, string>(options), value.Value);
    }
}
