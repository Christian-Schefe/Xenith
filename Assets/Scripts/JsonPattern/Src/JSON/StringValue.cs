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

            sb.Append('"');
            if (string.IsNullOrEmpty(value))
            {
                sb.Append('"');
                return;
            }

            foreach (char c in value)
            {
                switch (c)
                {
                    case '"':
                        sb.Append("\\\"");
                        break;
                    case '\\':
                        sb.Append("\\\\");
                        break;
                    case '\b':
                        sb.Append("\\b");
                        break;
                    case '\f':
                        sb.Append("\\f");
                        break;
                    case '\n':
                        sb.Append("\\n");
                        break;
                    case '\r':
                        sb.Append("\\r");
                        break;
                    case '\t':
                        sb.Append("\\t");
                        break;
                    default:
                        if (char.IsControl(c))
                        {
                            sb.Append("\\u");
                            sb.Append(((int)c).ToString("x4"));
                        }
                        else sb.Append(c);
                        break;
                }
            }
            sb.Append('"');
        }
    }
}
