namespace JsonPattern
{
    public class NullableSchema : Schema<NullableSchemaValue>
    {
        private readonly Schema innerSchema;

        public NullableSchema(Schema innerSchema)
        {
            this.innerSchema = innerSchema;
        }

        internal override void DoDeserialization(JsonValue json, DeserializationContext ctx)
        {
            if (json is NullValue)
            {
                ctx.Okay(new NullableSchemaValue(null));
            }
            innerSchema.DoDeserialization(json, ctx);
            if (ctx.IsOkay)
            {
                ctx.Okay(new NullableSchemaValue(ctx.Take()));
            }
        }
    }

    public class DefaultSchema<T> : Schema<T> where T : SchemaValue
    {
        private readonly Schema<T> innerSchema;
        private readonly System.Func<T> defaultFactory;

        public DefaultSchema(Schema<T> innerSchema, System.Func<T> defaultFactory)
        {
            this.innerSchema = innerSchema;
            this.defaultFactory = defaultFactory;
        }

        internal override void DoDeserialization(JsonValue json, DeserializationContext ctx)
        {
            innerSchema.DoDeserialization(json, ctx);
            if (ctx.IsError)
            {
                ctx.Unerror();
                ctx.Okay(new NullableSchemaValue(defaultFactory()));
            }
        }
    }

    public class NullableSchemaValue : SchemaValue
    {
        public SchemaValue value;

        public NullableSchemaValue(SchemaValue value)
        {
            this.value = value;
        }

        public override JsonValue Serialize()
        {
            return value == null ? new NullValue() : value.Serialize();
        }

        public override T Get<T>(string path)
        {
            if (value == null)
            {
                if (path == string.Empty)
                {
                    return default;
                }
                else
                {
                    throw new System.InvalidOperationException($"Cannot access path on null value in {nameof(NullableSchemaValue)}.");
                }
            }
            return value.Get<T>(path);
        }
    }
}