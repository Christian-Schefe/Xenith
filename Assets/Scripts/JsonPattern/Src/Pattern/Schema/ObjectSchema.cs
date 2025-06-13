using System.Collections.Generic;
using System.Linq;

namespace JsonPattern
{
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
                    schema.DoDeserialization(jsonValue, ctx);
                    if (ctx.IsError) return;
                    result[key] = ctx.Take();
                }
                else if (schema is NullableSchema)
                {
                    result[key] = new NullableSchemaValue(null);
                }
                else
                {
                    ctx.Error(json, $"Missing required key '{key}' in ObjectSchema.");
                    return;
                }
            }
            ctx.Okay(new ObjectSchemaValue(result));
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

        public override T Get<T>(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new System.InvalidOperationException("ObjectSchemaValue does not support empty path access.");
            }
            int index = path.IndexOf('.');
            string key = index == -1 ? path : path[..index];
            if (values.TryGetValue(key, out var value))
            {
                var remainingPath = index == -1 ? string.Empty : path[(index + 1)..];
                return value.Get<T>(remainingPath);
            }
            else
            {
                throw new KeyNotFoundException($"Key '{key}' not found in ObjectSchemaValue.");
            }
        }
    }
}