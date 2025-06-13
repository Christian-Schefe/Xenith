using System.Text;

namespace JsonPattern
{
    public class BoolValue : JsonValue
    {
        public bool value;

        public BoolValue(bool value)
        {
            this.value = value;
        }

        protected override void Stringify(StringBuilder sb)
        {
            sb.Append(value ? "true" : "false");
        }
    }
}
