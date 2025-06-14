using System.Collections.Generic;
using System.Text;

namespace JsonPattern
{
    public abstract class JsonValue
    {
        protected abstract void Stringify(StringBuilder sb);

        public override string ToString()
        {
            var sb = new StringBuilder();
            Stringify(sb);
            return sb.ToString();
        }

        public static bool TryParse(string json, out JsonValue value)
        {
            return TryParse(json, out value, out _);
        }

        public static bool TryParse(string json, out JsonValue value, out JsonSourceMap sourceMap)
        {
            if (string.IsNullOrEmpty(json))
            {
                value = null;
                sourceMap = null;
                return false;
            }
            var ctx = new ParseContext(json);
            ctx.SkipWhitespace();
            sourceMap = ctx.SourceMap;
            return TryParseNoWhitespace(ctx, out value);
        }

        private static bool TryParseNoWhitespace(ParseContext ctx, out JsonValue value)
        {
            if (!ctx.HasChar)
            {
                value = null;
                return false;
            }

            var currentChar = ctx.CurrentChar;
            JsonValue result = currentChar switch
            {
                'n' => TryParseNull(ctx, out var nullVal) ? nullVal : null,
                't' => TryParseBool(ctx, true, out var boolVal) ? boolVal : null,
                'f' => TryParseBool(ctx, false, out var boolVal) ? boolVal : null,
                '-' or '+' or (>= '0' and <= '9') => TryParseNumber(ctx, out var numVal) ? numVal : null,
                '"' => TryParseString(ctx, out var strVal) ? strVal : null,
                '[' => TryParseArray(ctx, out var arrayVal) ? arrayVal : null,
                '{' => TryParseObject(ctx, out var objVal) ? objVal : null,
                _ => null,
            };
            if (result != null)
            {
                value = result;
                return true;
            }
            else
            {
                value = null;
                return false;
            }
        }

        private static bool TryParseNull(ParseContext ctx, out NullValue value)
        {
            ctx.PushSourcePosition();
            if (ctx.TryMatchAndAdvance("null"))
            {
                value = new NullValue();
                ctx.AddSourcePosition(value);
                return true;
            }
            else
            {
                value = null;
                ctx.DiscardSourcePosition();
                return false;
            }
        }

        private static bool TryParseBool(ParseContext ctx, bool target, out BoolValue value)
        {
            ctx.PushSourcePosition();
            string boolStr = target ? "true" : "false";
            if (ctx.TryMatchAndAdvance(boolStr))
            {
                value = new BoolValue(target);
                ctx.AddSourcePosition(value);
                return true;
            }
            else
            {
                value = null;
                ctx.DiscardSourcePosition();
                return false;
            }
        }

        private static readonly List<char> numberChars = new() { '-', '+', '.', 'e', 'E' };

        private static bool TryParseNumber(ParseContext ctx, out NumberValue value)
        {
            ctx.PushSourcePosition();
            string numberStr = ctx.AdvanceWhile(c => char.IsDigit(c) || numberChars.Contains(c));
            if (double.TryParse(numberStr, out double number))
            {
                value = new NumberValue(number);
                ctx.AddSourcePosition(value);
                return true;
            }
            else
            {
                value = null;
                ctx.DiscardSourcePosition();
                return false;
            }
        }

        private static bool TryParseString(ParseContext ctx, out StringValue value)
        {
            ctx.PushSourcePosition();
            if (!ctx.TryMatchAndAdvance('"'))
            {
                value = null;
                ctx.DiscardSourcePosition();
                return false;
            }

            var sb = new StringBuilder();
            while (ctx.HasChar)
            {
                char c = ctx.GetAndAdvance();
                if (c == '"')
                {
                    value = new StringValue(sb.ToString());
                    ctx.AddSourcePosition(value);
                    return true;
                }
                else if (c == '\\')
                {
                    if (!ctx.HasChar) break;
                    c = ctx.GetAndAdvance();
                    char? toAppend = c switch
                    {
                        '"' => '"',
                        '\\' => '\\',
                        '/' => '/',
                        'b' => '\b',
                        'f' => '\f',
                        'n' => '\n',
                        'r' => '\r',
                        't' => '\t',
                        'u' => null,
                        _ => c
                    };
                    if (toAppend is char appendChar)
                    {
                        sb.Append(appendChar);
                    }
                    else if (ctx.TryGetAndAdvance(4, out var intStr) && int.TryParse(intStr, System.Globalization.NumberStyles.HexNumber, null, out int codePoint))
                    {
                        sb.Append((char)codePoint);
                    }
                    else break;
                }
                else
                {
                    sb.Append(c);
                }
            }
            value = null;
            ctx.DiscardSourcePosition();
            return false;
        }

        private static bool TryParseArray(ParseContext ctx, out JsonValue value)
        {
            ctx.PushSourcePosition();
            if (!ctx.TryMatchAndAdvance('['))
            {
                value = null;
                return false;
            }

            var array = new List<JsonValue>();
            while (ctx.HasChar)
            {
                ctx.SkipWhitespace();
                if (ctx.TryMatchAndAdvance(']'))
                {
                    value = new ArrayValue(array);
                    ctx.AddSourcePosition(value);
                    return true;
                }
                if (!TryParseNoWhitespace(ctx, out JsonValue item)) break;
                array.Add(item);
                ctx.SkipWhitespace();
                ctx.TryMatchAndAdvance(',');
            }

            value = null;
            ctx.DiscardSourcePosition();
            return false;
        }

        private static bool TryParseObject(ParseContext ctx, out JsonValue value)
        {
            ctx.PushSourcePosition();
            if (!ctx.TryMatchAndAdvance('{'))
            {
                value = null;
                ctx.DiscardSourcePosition();
                return false;
            }

            var obj = new Dictionary<string, JsonValue>();
            while (ctx.HasChar)
            {
                ctx.SkipWhitespace();
                if (ctx.TryMatchAndAdvance('}'))
                {
                    value = new ObjectValue(obj);
                    ctx.AddSourcePosition(value);
                    return true;
                }
                if (!TryParseString(ctx, out StringValue key)) break;
                ctx.SkipWhitespace();
                if (!ctx.TryMatchAndAdvance(':')) break;
                ctx.SkipWhitespace();
                if (!TryParseNoWhitespace(ctx, out JsonValue item)) break;
                obj[key.value] = item;
                ctx.SkipWhitespace();
                ctx.TryMatchAndAdvance(',');
            }

            value = null;
            ctx.DiscardSourcePosition();
            return false;
        }

        private class ParseContext
        {
            public readonly string json;
            private readonly JsonSourceMap sourceMap;
            private int position;
            private int line;
            private int column;

            private readonly Stack<JsonSourcePosition> markedPositions;

            public JsonSourceMap SourceMap => sourceMap;
            public int Position => position;
            public bool HasChar => position < json.Length;
            public char CurrentChar => json[position];

            public ParseContext(string json)
            {
                this.json = json;
                sourceMap = new();
                position = 0;
                line = 1;
                column = 1;
                markedPositions = new();
            }

            public char GetAndAdvance()
            {
                char c = json[position];
                AdvanceUnchecked();
                return c;
            }

            public bool TryGetAndAdvance(int count, out string str)
            {
                if (position + count > json.Length)
                {
                    str = null;
                    return false;
                }
                str = json.Substring(position, count);
                Advance(count);
                return true;
            }

            private void AdvanceUnchecked()
            {
                if (json[position] == '\n')
                {
                    line++;
                    column = 1;
                }
                else
                {
                    column++;
                }
                position++;
            }

            public void Advance(int count)
            {
                for (int i = 0; i < count && position < json.Length; i++)
                {
                    AdvanceUnchecked();
                }
            }

            public void PushSourcePosition()
            {
                markedPositions.Push(new JsonSourcePosition(line, column));
            }

            public void AddSourcePosition(JsonValue value)
            {
                var pos = markedPositions.TryPop(out var stackPos) ? stackPos : new JsonSourcePosition(line, column);
                sourceMap.Add(value, pos);
            }

            public void DiscardSourcePosition()
            {
                if (markedPositions.Count > 0)
                {
                    markedPositions.Pop();
                }
            }

            public void SkipWhitespace()
            {
                while (position < json.Length && char.IsWhiteSpace(json[position]))
                {
                    AdvanceUnchecked();
                }
            }

            public bool TryMatchAndAdvance(char c)
            {
                if (position < json.Length && json[position] == c)
                {
                    AdvanceUnchecked();
                    return true;
                }
                return false;
            }

            public bool TryMatchAndAdvance(string str)
            {
                if (json.Length < position + str.Length || json.Substring(position, str.Length) != str)
                {
                    return false;
                }
                Advance(str.Length);
                return true;
            }

            public string AdvanceWhile(System.Predicate<char> predicate)
            {
                int start = position;
                while (position < json.Length && predicate(json[position]))
                {
                    AdvanceUnchecked();
                }
                return json[start..position];
            }
        }
    }
}
