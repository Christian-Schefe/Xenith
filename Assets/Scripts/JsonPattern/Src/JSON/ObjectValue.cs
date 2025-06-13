using System.Collections.Generic;
using System.Text;

namespace JsonPattern
{
    public class ObjectValue : JsonValue
    {
        public Dictionary<string, JsonValue> values;

        public ObjectValue(Dictionary<string, JsonValue> values)
        {
            this.values = values;
        }

        protected override void Stringify(StringBuilder sb)
        {
            bool first = true;
            sb.Append("{");
            foreach (var kvp in values)
            {
                if (first) first = false;
                else sb.Append(", ");
                sb.Append(kvp.Key);
                sb.Append(":");
                sb.Append(kvp.Value.ToString());
            }
            sb.Append("}");
        }
    }
}
