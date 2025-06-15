using System.Text;

namespace JsonPattern
{
    public class StringValue : JsonValue
    {
        public string value;

        public StringValue(string value)
        {
            this.value = value;
        }

        protected override void Stringify(StringBuilder sb)
        {
            if (value == null)
            {
                sb.Append("null");
                return;
            }

            ToJsonString(value, sb);
        }
    }
}
