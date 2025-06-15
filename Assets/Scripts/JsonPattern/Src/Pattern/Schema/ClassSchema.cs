namespace JsonPattern
{
    /// <summary>
    /// A base class for schemas that represent a class-like structure in JSON.
    /// Accepts an object with a fixed set of keys, each associated with a specific schema.
    /// </summary>
    public abstract class ClassSchema : ObjectSchema
    {
        protected abstract (string key, Schema val)[] Values { get; }

        public ClassSchema() : base()
        {
            foreach (var (key, val) in Values)
            {
                values.Add(key, val);
            }
        }
    }

    public class ClassProp<T> where T : SchemaValue
    {
        public string name;
        public Schema<T> schema;

        public ClassProp(string name, Schema<T> schema)
        {
            this.name = name;
            this.schema = schema;
        }

        public (string, T) Make(T val) => (name, val);
        public T Retrieve(SchemaValue val) => (T)((ObjectSchemaValue)val).Values[name];
        public (string, Schema) Key => (name, schema);
    }
}