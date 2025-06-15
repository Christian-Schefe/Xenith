using System.Collections.Generic;
using System.Linq;

namespace JsonPattern
{
    /// <summary>
    /// Accepts a tuple of values, each conforming to a specified schema.
    /// </summary>
    public class TupleSchema : JsonSchema<ArraySchemaValue, ArrayValue>
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
            ctx.Push(new ArraySchemaValue(result));
        }
    }
}