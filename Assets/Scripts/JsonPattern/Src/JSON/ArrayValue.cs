using System.Collections.Generic;
using System.Text;

namespace JsonPattern
{
    public class ArrayValue : JsonValue
    {
        public List<JsonValue> values;

        public ArrayValue(List<JsonValue> values)
        {
            this.values = values;
        }

        protected override void Stringify(StringBuilder sb)
        {
            bool first = true;
            sb.Append("[");
            foreach (var val in values)
            {
                if (first) first = false;
                else sb.Append(",");
                sb.Append(val.ToString());
            }
            sb.Append("]");
        }
    }
}
