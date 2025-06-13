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
                new IntSchema().Optional(() => new(0)),
                new IntSchema().Optional(() => new(5))
            ))
        );

        public void Test()
        {
            var res = appSchema.TryDeserialize("{\"int1\": 44.0, \"min1\": 1, \"arr\": [\"hi\", \"a3897aad\", null, null]}", out var val);
            Debug.Log($"Deserialize result: {res}");
            if (res) Debug.Log(val.Get<int>("arr.2"));
            if (res) Debug.Log(val.Get<int>("arr.3"));
            if (res) Debug.Log(val.Get<int>("opt1"));
            if (res) Debug.Log(val.Serialize().ToString());

            var res2 = appSchema.TryDeserialize("{\"int1\": 440.00, \"min1\":-1, \"arr\": [\"hi\", \"a3897aad\", null, null]}", out _, out var err);
            Debug.Log($"Deserialize result: {res2}");
            if (!res2) Debug.Log(err);
        }
    }
}
