using System.Collections.Generic;

namespace JsonPattern
{
    internal class DeserializationContext
    {
        public JsonSourceMap sourceMap;
        private readonly Stack<SchemaValue> results;
        private readonly List<string> path;
        private bool isError;
        private string errorMessage;
        private string errorPath;
        private JsonSourcePosition? errorPosition;

        public bool IsOkay => !isError;
        public bool IsError => isError;
        private string ErrorHeader => $"Error at /{errorPath}{(errorPosition is JsonSourcePosition p ? $" [{p.line}:{p.column}]" : "")}";
        public string ErrorMessage => $"{ErrorHeader}: {errorMessage}";

        public DeserializationContext(JsonSourceMap sourceMap)
        {
            this.sourceMap = sourceMap;
            results = new();
            path = new();
        }

        public void Enter(string name)
        {
            path.Add(name);
        }

        public void Exit()
        {
            path.RemoveAt(path.Count - 1);
        }

        public SchemaValue Pop()
        {
            return results.Pop();
        }

        public void Push(SchemaValue result)
        {
            results.Push(result);
        }

        public void Unerror()
        {
            isError = false;
            errorMessage = null;
            errorPosition = default;
            errorPath = null;
        }

        public void Error(JsonValue value, string errorMessage)
        {
            isError = true;
            this.errorMessage = errorMessage;
            errorPosition = sourceMap?.Get(value);
            errorPath = string.Join('.', path);
        }
    }
}