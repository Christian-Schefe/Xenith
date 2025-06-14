using System.Collections.Generic;
using System.Linq;

namespace JsonPattern
{
    /// <summary>
    /// Accepts a tuple of values, each conforming to a specified schema.
    /// </summary>
    public class TupleSchema : JsonSchema<TupleSchemaValue, ArrayValue>
    {
        private readonly List<Schema> values;

        public TupleSchema(List<Schema> values)
        {
            this.values = values;
        }

        public TupleSchema(params Schema[] values)
        {
            this.values = values.ToList();
        }

        internal override void DoDeserialization(ArrayValue json, DeserializationContext ctx)
        {
            if (json.values.Count != values.Count)
            {
                ctx.Error(json, $"Expected {values.Count} elements in array, but got {json.values.Count}.");
                return;
            }

            var result = new List<SchemaValue>();
            for (int i = 0; i < values.Count; i++)
            {
                var schema = values[i];
                var val = json.values[i];
                ctx.Enter(i.ToString());
                schema.DoDeserialization(val, ctx);
                ctx.Exit();
                if (ctx.IsError) return;
                result.Add(ctx.Pop());
            }
            ctx.Push(new TupleSchemaValue(result));
        }
    }

    public class TupleSchemaValue : SchemaValue
    {
        private readonly List<SchemaValue> values;

        public int Count => values.Count;

        public TupleSchemaValue(List<SchemaValue> values)
        {
            this.values = values;
        }

        public override JsonValue Serialize()
        {
            return new ArrayValue(values.Select(e => e.Serialize()).ToList());
        }

        public SchemaValue this[int key] => values[key];

        public override bool TryGet(string path, out SchemaValue val)
        {
            if (base.TryGet(path, out val)) return true;

            SplitPath(path, out var key, out var remaining, out var isOptional);
            if (int.TryParse(key, out int keyIndex) && keyIndex >= 0 && keyIndex < values.Count)
            {
                var value = values[keyIndex];
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