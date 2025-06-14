using System.Collections.Generic;
using System.Linq;

namespace JsonPattern
{
    /// <summary>
    /// Accepts the first schema that matches the JSON value.
    /// </summary>
    public class OrSchema : Schema<TupleSchemaValue>
    {
        private readonly List<Schema> options;

        public OrSchema(List<Schema> options)
        {
            this.options = options;
        }

        public OrSchema(params Schema[] options)
        {
            this.options = options.ToList();
        }

        internal override void DoDeserialization(JsonValue json, DeserializationContext ctx)
        {
            var result = new List<SchemaValue>();
            bool hasResult = false;
            for (int i = 0; i < options.Count; i++)
            {
                if (hasResult)
                {
                    result.Add(new NullSchemaValue());
                    continue;
                }
                var schema = options[i];
                ctx.Enter(i.ToString());
                schema.DoDeserialization(json, ctx);
                ctx.Exit();
                if (ctx.IsOkay)
                {
                    result.Add(ctx.Pop());
                    hasResult = true;
                }
                else
                {
                    result.Add(new NullSchemaValue());
                    ctx.Unerror();
                }
            }
            if (!hasResult)
            {
                ctx.Error(json, $"No matching schema found for JSON value: {json}");
            }
            else
            {
                ctx.Push(new TupleSchemaValue(result));
            }
        }
    }
}