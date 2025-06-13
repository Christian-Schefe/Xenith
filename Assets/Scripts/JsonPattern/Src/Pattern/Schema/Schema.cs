using System.Collections.Generic;

namespace JsonPattern
{
    public abstract class Schema
    {
        internal abstract void DoDeserialization(JsonValue json, DeserializationContext ctx);
        public NullableSchema Nullable() => new(this);

        public bool TryDeserialize(string json, out SchemaValue value)
        {
            return TryDeserialize(json, out value, out _);
        }

        public bool TryDeserialize(string json, out SchemaValue value, out string error)
        {
            if (JsonValue.TryParse(json, out var jsonValue, out var sourceMap))
            {
                var ctx = new DeserializationContext(sourceMap);
                DoDeserialization(jsonValue, ctx);
                value = ctx.IsOkay ? ctx.Take() : null;
                error = ctx.IsOkay ? null : ctx.errorMessage;
                return ctx.IsOkay;
            }
            else
            {
                value = null;
                error = "Invalid JSON format.";
                return false;
            }
        }
    }

    public abstract class Schema<T> : Schema where T : SchemaValue
    {
        public DefaultSchema<T> Optional(System.Func<T> defaultValue) => new(this, defaultValue);
        public DefaultSchema<T> Optional<U>() where U : T, new() => new(this, () => new U());
    }

    public abstract class JsonSchema<T, J> : Schema<T> where T : SchemaValue where J : JsonValue
    {
        internal override void DoDeserialization(JsonValue json, DeserializationContext ctx)
        {
            if (json is J j)
            {
                DoDeserialization(j, ctx);
            }
            else
            {
                ctx.Error(json, $"Expected JSON of type {typeof(J).Name}, but got {json.GetType().Name}.");
            }
        }

        internal abstract void DoDeserialization(J json, DeserializationContext ctx);
    }

    public abstract class SchemaValue
    {
        public abstract JsonValue Serialize();
        public abstract T Get<T>(string path);
    }

    internal class DeserializationContext
    {
        public JsonSourceMap sourceMap;
        public Stack<SchemaValue> results;
        private bool isError;
        public string errorMessage;
        public JsonSourcePosition errorPosition;

        public bool IsOkay => !isError;
        public bool IsError => isError;

        public DeserializationContext(JsonSourceMap sourceMap)
        {
            this.sourceMap = sourceMap;
            results = new();
        }

        public SchemaValue Take()
        {
            return results.Pop();
        }

        public void Okay(SchemaValue result)
        {
            results.Push(result);
        }

        public void Unerror()
        {
            isError = false;
            errorMessage = null;
            errorPosition = default;
        }

        public void Error(JsonValue value, string errorMessage)
        {
            isError = true;
            this.errorMessage = errorMessage;
            errorPosition = sourceMap.Get(value);
        }
    }
}