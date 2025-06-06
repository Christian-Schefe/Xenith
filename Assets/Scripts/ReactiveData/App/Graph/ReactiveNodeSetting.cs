using ReactiveData.Core;
using System;

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

        public ReactiveNodeSetting(string name)
        {
            this.name = new(name);
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
    }

    public class ReactiveFloatSetting : ReactiveValueSetting<float>
    {
        public ReactiveFloatSetting(string name, float value) : base(name, value) { }
        public override ReactiveSettingType Type => ReactiveSettingType.Float;

        public override string Serialize() => value.Value.ToString("R");
        public override void Deserialize(string str) => value.Value = float.TryParse(str, out var result) ? result : 0f;
    }

    public class ReactiveStringSetting : ReactiveValueSetting<string>
    {
        public ReactiveStringSetting(string name, string value) : base(name, value) { }
        public override ReactiveSettingType Type => ReactiveSettingType.String;

        public override string Serialize() => value.Value;
        public override void Deserialize(string str) => value.Value = str ?? string.Empty;
    }

    public class ReactiveEnumSetting : ReactiveValueSetting<int>
    {
        public Type type;

        public ReactiveEnumSetting(string name, Type type, int value) : base(name, value)
        {
            this.type = type;
        }

        public override ReactiveSettingType Type => ReactiveSettingType.Enum;

        public override string Serialize() => value.ToString();
        public override void Deserialize(string str) => value.Value = int.TryParse(str, out var result) ? result : 0;
    }

    public class ReactiveEnumSetting<T> : ReactiveEnumSetting where T : struct, IConvertible
    {
        public ReactiveEnumSetting(string name, T value) : base(name, typeof(T), 0)
        {
            if (!typeof(T).IsEnum)
            {
                throw new ArgumentException("T must be of type Enum");
            }
            if (Enum.GetUnderlyingType(typeof(T)) != typeof(int))
            {
                throw new ArgumentException("Enum must have int as underlying type.");
            }
            this.value.Value = Convert.ToInt32(value);
        }
    }
}
