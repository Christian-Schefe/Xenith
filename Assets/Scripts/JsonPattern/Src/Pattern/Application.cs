using UnityEngine;

namespace JsonPattern
{
    public class TestApplication
    {
        public Schema appSchema = new ObjectSchema(
            ("int1", new IntSchema()),
            ("min1", new IntSchema().Min(0)),
            ("opt1", new IntSchema().Optional(() => new(5))),
            ("arr", new TupleSchema(
                new StringSchema(),
                new StringSchema().Matches("a[0-9]+a"),
                new IntSchema().Nullable(),
                new IntSchema().Optional(() => new(5))
            )),
            ("or", new OrSchema(
                new StringSchema().Array(),
                new IntSchema().Nullable(),
                new TestClassSchema()
            ).Nullable())
        );

        public void Test()
        {
            var res = appSchema.TryDeserialize("{\"int1\": 44.0, \"min1\": 1, \"arr\": [\"hi\", \"a3897aad\", null, null], \"or\":{\"int1\":100,\"s1\":\"h___i\"}}", out var val, out var err);

            Debug.Log($"Deserialize result: {res}");
            if (res) Debug.Log(val.Serialize().ToString());
            else Debug.Log(err);

            if (res) Debug.Log(val.AsNullable<IntSchemaValue>("arr.2")?.Value);
            if (res) Debug.Log(val.As<IntSchemaValue>("arr.3").Value);
            if (res) Debug.Log(val.As<IntSchemaValue>("opt1").Value);
            if (res) Debug.Log(val.As<StringSchemaValue>("or.2.s1").Value);
            if (res) Debug.Log(val.AsNullable<StringSchemaValue>("or.2.recursive?.s1")?.Value);
            if (res) Debug.Log(val.As<ObjectSchemaValue>("or.2")["s1"]);


            var res2 = appSchema.TryDeserialize("{\"int1\": 440.00, \"min1\":1, \"arr\": [\"hi\", \"a3897aad\", \"null\", null]}", out var val2, out var err2);

            Debug.Log($"Deserialize result: {res2}");
            if (res2) Debug.Log(val2.Serialize().ToString());
            else Debug.Log(err2);
        }

        public void Test2()
        {
            var res = appSchema.TryDeserialize("{\"int1\": 44.0, \"min1\": 1, \"arr\": [\"hi\", \"a3897aad\", null, null], \"or\":[\"a\", \"b\"]}", out var val, out var err);

            Debug.Log($"Deserialize result: {res}");
            if (res) Debug.Log(val.Serialize().ToString());
            else Debug.Log(err);

            if (res) Debug.Log(val.AsNullable<StringSchemaValue>("or.2?.s1")?.Value);
            if (res) Debug.Log(val.AsNullable<StringSchemaValue>("or.2?.recursive?.s1")?.Value);
            if (res) Debug.Log(val.As<TupleSchemaValue>("or.0").Count);
            if (res) Debug.Log(val.As<StringSchemaValue>("or.0.1").Value);
        }
    }

    public class TestClassSchema : ClassSchema
    {
        public IntSchema int1 = new IntSchema().Range(5, 100);
        public StringSchema s1 = new StringSchema().Matches("^h_*i$");
        public static Schema recursive = new TestClassSchema().Nullable();

        protected override (string, Schema)[] Values => new (string, Schema)[] {
            (nameof(int1), int1),
            (nameof(s1), s1),
            (nameof(recursive), recursive)
        };
    }
}
