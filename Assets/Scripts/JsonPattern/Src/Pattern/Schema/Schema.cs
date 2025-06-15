namespace JsonPattern
{
    public abstract class Schema
    {
        internal abstract void DoDeserialization(JsonValue json, DeserializationContext ctx);

        private bool TryDeserialize(JsonValue json, DeserializationContext ctx, out SchemaValue value, out string error)
        {
            DoDeserialization(json, ctx);
            value = ctx.IsOkay ? ctx.Pop() : null;
            error = ctx.IsOkay ? null : ctx.ErrorMessage;
            return ctx.IsOkay;
        }

        public bool TryDeserialize(JsonValue json, out SchemaValue value, out string error)
        {
            var ctx = new DeserializationContext(null);
            return TryDeserialize(json, ctx, out value, out error);
        }

        public bool TryDeserialize(string json, out SchemaValue value)
        {
            return TryDeserialize(json, out value, out _);
        }

        public bool TryDeserialize(string json, out SchemaValue value, out string error)
        {
            if (JsonValue.TryParse(json, out var jsonValue, out var sourceMap))
            {
                var ctx = new DeserializationContext(sourceMap);
                return TryDeserialize(jsonValue, ctx, out value, out error);
            }
            else
            {
                value = null;
                error = "Invalid JSON format.";
                return false;
            }
        }

        public NullableSchema Nullable() => new(this);
        public ArraySchema Array() => new(this);
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

        public virtual bool TryGet(string path, out SchemaValue val)
        {
            if (string.IsNullOrEmpty(path))
            {
                val = this;
                return true;
            }
            val = null;
            return false;
        }

        protected static void SplitPath(string path, out string key, out string remaining, out bool isOptional)
        {
            int index = path.IndexOf('.');
            if (index == -1)
            {
                key = path;
                remaining = string.Empty;
                isOptional = false;
                return;
            }
            isOptional = false;
            remaining = path[(index + 1)..];
            if (index > 0 && path[index - 1] == '?')
            {
                isOptional = true;
                index--;
            }
            key = path[..index];
        }

        public T As<T>(string path) where T : SchemaValue
        {
            if (TryGet(path, out var val))
            {
                if (val is T t) return t;
                throw new System.InvalidCastException($"Cannot cast value at path '{path}' from type {val.GetType().Name} to type {typeof(T).Name}.");
            }
            throw new System.ArgumentException($"Path '{path}' not found in SchemaValue.");
        }

        public T AsNullable<T>(string path) where T : SchemaValue
        {
            if (TryGet(path, out var val))
            {
                if (val is T t) return t;
                else if (val is NullSchemaValue) return null;
                throw new System.InvalidCastException($"Cannot cast value at path '{path}' from type {val.GetType().Name} to type {typeof(T).Name}.");
            }
            throw new System.ArgumentException($"Path '{path}' not found in SchemaValue.");
        }

        public bool IsNotNull(string path = "")
        {
            if (TryGet(path, out var val))
            {
                if (val is NullSchemaValue) return false;
                else return true;
            }
            throw new System.ArgumentException($"Path '{path}' not found in SchemaValue.");
        }

        public T As<T>() where T : SchemaValue => As<T>(string.Empty);
    }
}
