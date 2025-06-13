using System.Collections.Generic;
using System.Linq;

namespace JsonPattern
{
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
                schema.DoDeserialization(val, ctx);
                if (ctx.IsError) return;
                result.Add(ctx.Take());
            }
            ctx.Okay(new TupleSchemaValue(result));
        }
    }

    public class TupleSchemaValue : SchemaValue
    {
        public List<SchemaValue> values;

        public TupleSchemaValue(List<SchemaValue> values)
        {
            this.values = values;
        }

        public override JsonValue Serialize()
        {
            return new ArrayValue(values.Select(e => e.Serialize()).ToList());
        }

        public override T Get<T>(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new System.InvalidOperationException("ObjectSchemaValue does not support empty path access.");
            }
            int index = path.IndexOf('.');
            string key = index == -1 ? path : path[..index];
            if (!int.TryParse(key, out int keyIndex) || keyIndex < 0 || keyIndex >= values.Count)
            {
                throw new System.IndexOutOfRangeException($"Index '{key}' is out of range for TupleSchemaValue with {values.Count} elements.");
            }
            var remainingPath = index == -1 ? string.Empty : path[(index + 1)..];
            return values[keyIndex].Get<T>(remainingPath);
        }
    }
}