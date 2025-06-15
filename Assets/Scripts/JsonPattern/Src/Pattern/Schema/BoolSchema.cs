namespace JsonPattern
{
    /// <summary>
    /// Accepts a boolean value.
    /// </summary>
    public class BoolSchema : JsonSchema<BoolSchemaValue, BoolValue>
    {
        internal override void DoDeserialization(BoolValue json, DeserializationContext ctx)
        {
            ctx.Push(new BoolSchemaValue(json.value));
        }
    }

    public class BoolSchemaValue : SchemaValue
    {
        private bool value;
        public bool Value
        {
            get => value;
            set => this.value = value;
        }

        public BoolSchemaValue(bool value)
        {
            this.value = value;
        }

        public override JsonValue Serialize()
        {
            return new BoolValue(value);
        }
    }
}