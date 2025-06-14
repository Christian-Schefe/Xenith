using System.Collections.Generic;

namespace JsonPattern
{
    /// <summary>
    /// Accepts an array of values, each conforming to a specified element schema.
    /// </summary>
    public class ArraySchema : JsonSchema<TupleSchemaValue, ArrayValue>
    {
        private readonly Schema elementSchema;

        public ArraySchema(Schema elementSchema)
        {
            this.elementSchema = elementSchema;
        }

        internal override void DoDeserialization(ArrayValue json, DeserializationContext ctx)
        {
            var result = new List<SchemaValue>();
            for (int i = 0; i < json.values.Count; i++)
            {
                var val = json.values[i];
                ctx.Enter(i.ToString());
                elementSchema.DoDeserialization(val, ctx);
                ctx.Exit();
                if (ctx.IsError) return;
                result.Add(ctx.Pop());
            }
            ctx.Push(new TupleSchemaValue(result));
        }
    }
}