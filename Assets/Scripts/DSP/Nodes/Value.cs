using System;

namespace DSP
{
    public abstract class NamedValue
    {
        public string name;
        public abstract Value Value { get; }

        public NamedValue(string name)
        {
            this.name = name;
        }

        public abstract NamedValue Clone();

        public void Set(NamedValue from)
        {
            Value.Set(from.Value);
        }
    }

    public class NamedValue<T> : NamedValue where T : Value
    {
        public T value;

        public override Value Value => value;

        public NamedValue(string name, T value) : base(name)
        {
            this.value = value;
        }

        public override NamedValue Clone()
        {
            return new NamedValue<T>(name, (T)value.Clone());
        }
    }

    public enum ValueType
    {
        Float,
        Bool
    }

    public abstract class Value
    {
        public abstract ValueType Type { get; }
        public abstract Value Clone();
        public abstract void Set(Value value);

        public static Value NewFromType(ValueType type)
        {
            return type switch
            {
                ValueType.Float => new FloatValue(),
                ValueType.Bool => new BoolValue(),
                _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
            };
        }
    }

    public abstract class BaseValue<T> : Value
    {
        public T value;

        protected BaseValue(T value)
        {
            this.value = value;
        }

        public override void Set(Value value)
        {
            this.value = ((BaseValue<T>)value).value;
        }
    }

    public class FloatValue : BaseValue<float>
    {
        public FloatValue() : base(0f) { }
        public FloatValue(float value) : base(value) { }
        public override ValueType Type => ValueType.Float;

        public override Value Clone() => new FloatValue(value);
    }

    public class BoolValue : BaseValue<bool>
    {
        public BoolValue() : base(false) { }
        public BoolValue(bool value) : base(value) { }
        public override ValueType Type => ValueType.Bool;

        public override Value Clone() => new BoolValue(value);
    }
}
