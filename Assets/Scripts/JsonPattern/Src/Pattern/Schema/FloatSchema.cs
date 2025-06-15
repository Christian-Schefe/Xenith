namespace JsonPattern
{
    /// <summary>
    /// Accepts a float value.
    /// </summary>
    public class FloatSchema : JsonSchema<FloatSchemaValue, NumberValue>
    {
        internal override void DoDeserialization(NumberValue json, DeserializationContext ctx)
        {
            ctx.Push(new FloatSchemaValue((float)json.value));
        }
    }

    public class FloatSchemaValue : SchemaValue
    {
        private float value;
        public float Value
        {
            get => value;
            set => this.value = value;
        }

        public FloatSchemaValue(float value)
        {
            this.value = value;
        }

        public override JsonValue Serialize()
        {
            return new NumberValue(value);
        }
    }
}