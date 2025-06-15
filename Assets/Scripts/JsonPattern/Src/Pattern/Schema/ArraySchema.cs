using System.Collections.Generic;
using System.Linq;

namespace JsonPattern
{
    /// <summary>
    /// Accepts an array of values, each conforming to a specified element schema.
    /// </summary>
    public class ArraySchema<T> : JsonSchema<ArraySchemaValue<T>, ArrayValue> where T : SchemaValue
    {
        protected readonly Schema<T> elementSchema;

        public ArraySchema(Schema<T> elementSchema)
        {
            this.elementSchema = elementSchema;
        }

        internal override void DoDeserialization(ArrayValue json, DeserializationContext ctx)
        {
            var result = new List<T>();
            for (int i = 0; i < json.values.Count; i++)
            {
                var val = json.values[i];
                ctx.Enter(i.ToString());
                elementSchema.DoDeserialization(val, ctx);
                ctx.Exit();
                if (ctx.IsError) return;
                result.Add((T)ctx.Pop());
            }
            ctx.Push(new ArraySchemaValue<T>(result));
        }
    }

    /// <summary>
    /// Accepts an array of values, each conforming to a specified element schema.
    /// </summary>
    public class ArraySchema : JsonSchema<ArraySchemaValue, ArrayValue>
    {
        protected readonly Schema elementSchema;

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
            ctx.Push(new ArraySchemaValue(result));
        }
    }

    public abstract class BaseArraySchemaValue : SchemaValue
    {
        public abstract int Count { get; }
        protected abstract IEnumerable<SchemaValue> ValueEnumerable { get; }
        protected abstract bool TryGetAt(int index, out SchemaValue val);

        public override bool TryGet(string path, out SchemaValue val)
        {
            if (base.TryGet(path, out val)) return true;

            SplitPath(path, out var key, out var remaining, out var isOptional);
            if (int.TryParse(key, out int keyIndex) && TryGetAt(keyIndex, out var value))
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
        public override JsonValue Serialize()
        {
            return new ArrayValue(ValueEnumerable.Select(e => e.Serialize()).ToList());
        }
    }

    public class ArraySchemaValue : BaseArraySchemaValue
    {
        private readonly List<SchemaValue> values;
        public override int Count => values.Count;
        public List<SchemaValue> Values => values;

        protected override IEnumerable<SchemaValue> ValueEnumerable => values;

        protected override bool TryGetAt(int index, out SchemaValue val)
        {
            if (index >= 0 && index < values.Count)
            {
                val = values[index];
                return true;
            }
            val = null;
            return false;
        }

        public ArraySchemaValue(List<SchemaValue> values)
        {
            this.values = values;
        }
    }

    public class ArraySchemaValue<T> : BaseArraySchemaValue where T : SchemaValue
    {
        private readonly List<T> values;
        public override int Count => values.Count;
        public List<T> Values => values;

        protected override IEnumerable<SchemaValue> ValueEnumerable => values;

        protected override bool TryGetAt(int index, out SchemaValue val)
        {
            if (index >= 0 && index < values.Count)
            {
                val = values[index];
                return true;
            }
            val = null;
            return false;
        }

        public ArraySchemaValue(List<T> values)
        {
            this.values = values;
        }
    }
}