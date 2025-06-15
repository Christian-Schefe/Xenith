using Yeast;

namespace JsonPattern
{
    public class AutoSchema<T> : Schema<AutoSchemaValue<T>>
    {
        internal override void DoDeserialization(JsonValue json, DeserializationContext ctx)
        {
            if (!json.ToString().TryFromJson(out T value))
            {
                try
                {
                    json.ToString().FromJson<T>();
                }
                catch (System.Exception ex)
                {
                    ctx.Error(json, $"Failed to deserialize JSON ({json.ToString()}) to type {typeof(T).Name}: {ex.Message}\n{ex.StackTrace}.");
                    return;
                }
                ctx.Error(json, $"Failed to deserialize JSON ({json.ToString()}) to type {typeof(T).Name}.");
                return;
            }
            ctx.Push(new AutoSchemaValue<T>(value));
        }
    }

    public class AutoSchemaValue<T> : SchemaValue
    {
        private T value;
        public T Value
        {
            get => value;
            set => this.value = value;
        }

        public AutoSchemaValue(T value)
        {
            this.value = value;
        }

        public override JsonValue Serialize()
        {
            return JsonValue.TryParse(value.ToJson(), out var json) ? json : throw new System.Exception("Failed to serialize value to JSON.");
        }
    }
}