using System.Collections.Generic;
using System.Linq;

namespace JsonPattern
{
    /// <summary>
    /// Accepts an object with a fixed set of keys, each associated with a specific schema.
    /// </summary>
    public class ObjectSchema : JsonSchema<ObjectSchemaValue, ObjectValue>
    {
        private readonly Dictionary<string, Schema> values;

        public ObjectSchema(Dictionary<string, Schema> values)
        {
            this.values = values;
        }

        public ObjectSchema(params (string key, Schema val)[] values)
        {
            this.values = values.ToDictionary(e => e.key, e => e.val);
        }

        internal override void DoDeserialization(ObjectValue json, DeserializationContext ctx)
        {
            var result = new Dictionary<string, SchemaValue>();
            foreach (var (key, schema) in values)
            {
                if (json.values.TryGetValue(key, out var jsonValue))
                {
                    ctx.Enter(key);
                    schema.DoDeserialization(jsonValue, ctx);
                    ctx.Exit();
                    if (ctx.IsError) return;
                    result[key] = ctx.Pop();
                }
                else if (schema is IOptionalSchema optionalSchema)
                {
                    result[key] = optionalSchema.GetDefault();
                }
                else
                {
                    ctx.Error(json, $"Missing required key '{key}' in ObjectSchema.");
                    return;
                }
            }
            ctx.Push(new ObjectSchemaValue(result));
        }
    }

    public class ObjectSchemaValue : SchemaValue
    {
        private readonly Dictionary<string, SchemaValue> values;

        public ObjectSchemaValue(Dictionary<string, SchemaValue> values)
        {
            this.values = values;
        }

        public override JsonValue Serialize()
        {
            return new ObjectValue(values.ToDictionary(e => e.Key, e => e.Value.Serialize()));
        }

        public SchemaValue this[string key]
        {
            get
            {
                if (values.TryGetValue(key, out var value))
                {
                    return value;
                }
                throw new KeyNotFoundException($"Key '{key}' not found.");
            }
        }

        public override bool TryGet(string path, out SchemaValue val)
        {
            if (base.TryGet(path, out val)) return true;

            SplitPath(path, out var key, out var remaining, out var isOptional);
            if (values.TryGetValue(key, out var value))
            {
                if (isOptional && value is NullSchemaValue nVal)
                {
                    val = nVal;
                    return true;
                }
                return value.TryGet(remaining, out val);
            }
            val = null;
            return false;
        }
    }
}