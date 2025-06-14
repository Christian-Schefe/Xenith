using System.Collections.Generic;

namespace JsonPattern
{
    public class JsonSourceMap
    {
        private readonly Dictionary<JsonValue, JsonSourcePosition> sourceMap = new();

        public void Add(JsonValue value, JsonSourcePosition position)
        {
            sourceMap[value] = position;
        }

        public JsonSourcePosition Get(JsonValue value) => sourceMap[value];
    }

    public struct JsonSourcePosition
    {
        public int line;
        public int column;

        public JsonSourcePosition(int line, int column)
        {
            this.line = line;
            this.column = column;
        }

        public override readonly string ToString()
        {
            return $"Line: {line}, Column: {column}";
        }
    }
}