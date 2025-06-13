namespace JsonPattern
{
    public class IntSchema : JsonSchema<IntSchemaValue, NumberValue>
    {
        internal override void DoDeserialization(NumberValue json, DeserializationContext ctx)
        {
            bool isInt = json.value == (int)json.value;
            if (isInt) ctx.Okay(new IntSchemaValue((int)json.value));
            else ctx.Error(json, $"Expected an integer, but got {json.value}.");
        }

        public RangeInt Min(int minValue) => new(minValue, null, this);
        public RangeInt Max(int maxValue) => new(null, maxValue, this);
        public RangeInt Range(int minValue, int maxValue) => new(minValue, maxValue, this);
    }

    public class RangeInt : IntSchema
    {
        private readonly IntSchema inner;
        private readonly int? minValue;
        private readonly int? maxValue;

        public RangeInt(int? minValue, int? maxValue, IntSchema inner)
        {
            this.inner = inner;
            this.minValue = minValue;
            this.maxValue = maxValue;
        }

        internal override void DoDeserialization(NumberValue json, DeserializationContext ctx)
        {
            inner.DoDeserialization((JsonValue)json, ctx);
            if (ctx.IsOkay)
            {
                var num = (int)json.value;
                if (minValue is int min && num < min)
                {
                    ctx.Take();
                    ctx.Error(json, $"Value {num} is below minimum limit of {minValue}.");
                }
                else if (maxValue is int max && num > max)
                {
                    ctx.Take();
                    ctx.Error(json, $"Value {num} exceeds maximum limit of {maxValue}.");
                }
            }
        }
    }

    public class IntSchemaValue : SchemaValue
    {
        internal readonly int value;

        public IntSchemaValue(int value)
        {
            this.value = value;
        }

        public override JsonValue Serialize()
        {
            return new NumberValue(value);
        }

        public override T Get<T>(string path)
        {
            if (path != string.Empty)
            {
                throw new System.InvalidOperationException($"{nameof(IntSchemaValue)} does not support path access.");
            }
            else
            {
                try
                {
                    var targetType = System.Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T);
                    return (T)System.Convert.ChangeType(value, targetType);
                }
                catch
                {
                    throw new System.InvalidOperationException($"Cannot get value of type {typeof(T)} from {nameof(IntSchemaValue)}.");
                }
            }
        }
    }
}