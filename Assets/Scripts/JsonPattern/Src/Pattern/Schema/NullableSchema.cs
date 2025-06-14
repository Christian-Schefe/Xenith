namespace JsonPattern
{
    public interface IOptionalSchema
    {
        SchemaValue GetDefault();
    }

    /// <summary>
    /// Accepts a value that can either be null or conform to a specified inner schema.
    /// </summary>
    public class NullableSchema : Schema, IOptionalSchema
    {
        private readonly Schema innerSchema;

        public NullableSchema(Schema innerSchema)
        {
            this.innerSchema = innerSchema;
        }

        public SchemaValue GetDefault() => new NullSchemaValue();

        internal override void DoDeserialization(JsonValue json, DeserializationContext ctx)
        {
            if (json is NullValue)
            {
                ctx.Push(GetDefault());
                return;
            }
            innerSchema.DoDeserialization(json, ctx);
        }
    }

    /// <summary>
    /// Accepts a value that conforms to a specified inner schema, with a default value provided by a factory function.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class DefaultSchema<T> : Schema<T>, IOptionalSchema where T : SchemaValue
    {
        private readonly Schema<T> innerSchema;
        private readonly System.Func<T> defaultFactory;

        public DefaultSchema(Schema<T> innerSchema, System.Func<T> defaultFactory)
        {
            this.innerSchema = innerSchema;
            this.defaultFactory = defaultFactory;
        }

        public SchemaValue GetDefault() => defaultFactory();

        internal override void DoDeserialization(JsonValue json, DeserializationContext ctx)
        {
            innerSchema.DoDeserialization(json, ctx);
            if (ctx.IsError)
            {
                ctx.Unerror();
                ctx.Push(GetDefault());
            }
        }
    }

    public class NullSchemaValue : SchemaValue
    {
        public override JsonValue Serialize()
        {
            return new NullValue();
        }
    }
}