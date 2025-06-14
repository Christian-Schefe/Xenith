using System.Collections.Generic;
using System.Linq;

namespace JsonPattern
{
    /// <summary>
    /// A base class for schemas that represent a class-like structure in JSON.
    /// Accepts an object with a fixed set of keys, each associated with a specific schema.
    /// </summary>
    public abstract class ClassSchema : JsonSchema<ObjectSchemaValue, ObjectValue>
    {
        private readonly Dictionary<string, Schema> values;

        protected abstract (string key, Schema val)[] Values { get; }

        public ClassSchema()
        {
            values = Values.ToDictionary(e => e.key, e => e.val);
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
}