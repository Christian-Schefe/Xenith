using System.Text;

namespace JsonPattern
{
    public class NullValue : JsonValue
    {
        protected override void Stringify(StringBuilder sb)
        {
            sb.Append("null");
        }
    }
}
