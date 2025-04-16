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
        Float, Bool
    }

    public abstract class Value
    {
        public abstract Value Clone();
        public abstract void Set(Value value);
        public abstract ValueType Type { get; }
    }

    public class FloatValue : Value
    {
        public float value;

        public FloatValue()
        {
            value = 0;
        }

        public FloatValue(float value)
        {
            this.value = value;
        }

        public override ValueType Type => ValueType.Float;

        public override Value Clone()
        {
            return new FloatValue(value);
        }

        public override void Set(Value value)
        {
            this.value = ((FloatValue)value).value;
        }
    }

    public class BoolValue : Value
    {
        public bool value;

        public BoolValue()
        {
            value = false;
        }

        public BoolValue(bool value)
        {
            this.value = value;
        }

        public override ValueType Type => ValueType.Bool;

        public override Value Clone()
        {
            return new BoolValue(value);
        }

        public override void Set(Value value)
        {
            this.value = ((BoolValue)value).value;
        }
    }
}
