namespace JsonPattern
{
    /// <summary>
    /// Accepts a string value.
    /// </summary>
    public class StringSchema : JsonSchema<StringSchemaValue, StringValue>
    {
        internal override void DoDeserialization(StringValue json, DeserializationContext ctx)
        {
            ctx.Push(new StringSchemaValue(json.value));
        }

        public RegexStringSchema Matches(string pattern) => new(pattern, this);
    }

    /// <summary>
    /// Accepts a string value that matches a specified regular expression pattern.
    /// </summary>
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
                ctx.Pop();
                ctx.Error(json, $"String '{json.value}' does not match pattern '{pattern}'.");
            }
        }
    }

    public class StringSchemaValue : SchemaValue
    {
        private string value;
        public string Value
        {
            get => value;
            set
            {
                this.value = value ?? throw new System.ArgumentNullException(nameof(value), "String value cannot be null.");
            }
        }

        public StringSchemaValue(string value)
        {
            this.value = value;
        }

        public override JsonValue Serialize()
        {
            return new StringValue(value);
        }
    }
}