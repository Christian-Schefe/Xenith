using System.Collections.Generic;

namespace JsonPattern
{
    public abstract class Schema
    {
        internal abstract void DoDeserialization(JsonValue json, DeserializationContext ctx);

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
                value = ctx.IsOkay ? ctx.Pop() : null;
                error = ctx.IsOkay ? null : ctx.ErrorMessage;
                return ctx.IsOkay;
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

    internal class DeserializationContext
    {
        public JsonSourceMap sourceMap;
        private readonly Stack<SchemaValue> results;
        private readonly List<string> path;
        private bool isError;
        private string errorMessage;
        private string errorPath;
        private JsonSourcePosition errorPosition;

        public bool IsOkay => !isError;
        public bool IsError => isError;
        public string ErrorMessage => $"Error at /{errorPath} [{errorPosition.line}:{errorPosition.column}]: {errorMessage}";

        public DeserializationContext(JsonSourceMap sourceMap)
        {
            this.sourceMap = sourceMap;
            results = new();
            path = new();
        }

        public void Enter(string name)
        {
            path.Add(name);
        }

        public void Exit()
        {
            path.RemoveAt(path.Count - 1);
        }

        public SchemaValue Pop()
        {
            return results.Pop();
        }

        public void Push(SchemaValue result)
        {
            results.Push(result);
        }

        public void Unerror()
        {
            isError = false;
            errorMessage = null;
            errorPosition = default;
            errorPath = null;
        }

        public void Error(JsonValue value, string errorMessage)
        {
            isError = true;
            this.errorMessage = errorMessage;
            errorPosition = sourceMap.Get(value);
            errorPath = string.Join('.', path);
        }
    }
}