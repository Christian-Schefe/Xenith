namespace JsonPattern
{
    public class StringSchema : JsonSchema<StringSchemaValue, StringValue>
    {
        internal override void DoDeserialization(StringValue json, DeserializationContext ctx)
        {
            ctx.Okay(new StringSchemaValue(json.value));
        }

        public RegexStringSchema Matches(string pattern) => new(pattern, this);
    }

    public class RegexStringSchema : StringSchema
    {
        private readonly StringSchema inner;
        private readonly string pattern;

        public RegexStringSchema(string pattern, StringSchema inner)
        {
            this.pattern = pattern;
            this.inner = inner;
        }

        internal override void DoDeserialization(StringValue json, DeserializationContext ctx)
        {
            inner.DoDeserialization(json, ctx);
            if (ctx.IsOkay && !System.Text.RegularExpressions.Regex.IsMatch(json.value, pattern))
            {
                ctx.Take();
                ctx.Error(json, $"String '{json.value}' does not match pattern '{pattern}'.");
            }
        }
    }

    public class StringSchemaValue : SchemaValue
    {
        public string value;

        public StringSchemaValue(string value)
        {
            this.value = value;
        }

        public override JsonValue Serialize()
        {
            return new StringValue(value);
        }

        public override T Get<T>(string path)
        {
            if (path != string.Empty)
            {
                throw new System.InvalidOperationException($"{nameof(StringSchemaValue)} does not support path access.");
            }
            else if (typeof(T) == typeof(string))
            {
                return (T)(object)value;
            }
            else
            {
                throw new System.InvalidOperationException($"Cannot get value of type {typeof(T)} from {nameof(StringSchemaValue)}.");
            }
        }
    }
}