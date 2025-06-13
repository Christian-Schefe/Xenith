using System.Text;

namespace JsonPattern
{
    public class NumberValue : JsonValue
    {
        public double value;

        public NumberValue(double value)
        {
            this.value = value;
        }

        protected override void Stringify(StringBuilder sb)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
            {
                sb.Append("null");
                return;
            }

            sb.Append(value.ToString("R", System.Globalization.CultureInfo.InvariantCulture));
        }
    }
}
