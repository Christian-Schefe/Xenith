using NUnit.Framework;
using UnityEngine;
using JsonPattern;

public class ApplicationTest
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

    [Test]
    public void TestSuccess()
    {
        var res = appSchema.TryDeserialize("{\"int1\": 44.0, \"min1\": 1, \"arr\": [\"hi\", \"a3897aad\", null, null], \"or\":{\"int1\":100,\"s1\":\"h___i\"}}", out var val, out var err);
        Assert.IsTrue(res, "Expected deserialization to succeed with valid input.");
        Debug.Log($"Deserialize result: {res}");
        if (res) Debug.Log(val.Serialize().ToString());
        else Debug.Log(err);

        if (res) Debug.Log(val.AsNullable<IntSchemaValue>("arr.2")?.Value);
        if (res) Debug.Log(val.As<IntSchemaValue>("arr.3").Value);
        if (res) Debug.Log(val.As<IntSchemaValue>("opt1").Value);
        if (res) Debug.Log(val.As<StringSchemaValue>("or.2.s1").Value);
        if (res) Debug.Log(val.AsNullable<StringSchemaValue>("or.2.recursive?.s1")?.Value);
        if (res) Debug.Log(val.As<ObjectSchemaValue>("or.2").Values["s1"]);


        var res2 = appSchema.TryDeserialize("{\"int1\": 44.0, \"min1\": 1, \"arr\": [\"hi\", \"a3897aad\", null, null], \"or\":[\"a\", \"b\"]}", out var val2, out var err2);
        Assert.IsTrue(res2, "Expected deserialization to succeed with valid input for 'or' as an array.");
        Debug.Log($"Deserialize result: {res2}");
        if (res2) Debug.Log(val2.Serialize().ToString());
        else Debug.Log(err2);

        if (res2) Debug.Log(val2.AsNullable<StringSchemaValue>("or.2?.s1")?.Value);
        if (res2) Debug.Log(val2.AsNullable<StringSchemaValue>("or.2?.recursive?.s1")?.Value);
        if (res2) Debug.Log(val2.As<ArraySchemaValue>("or.0").Count);
        if (res2) Debug.Log(val2.As<StringSchemaValue>("or.0.1").Value);

    }

    [Test]
    public void TestFail()
    {
        var res = appSchema.TryDeserialize("{\"int1\": 440.00, \"min1\":1, \"arr\": [\"hi\", \"a3897aad\", \"null\", null]}", out var val, out var err);
        Assert.IsFalse(res, "Expected deserialization to fail due to invalid 'null' string in array.");
        Debug.Log($"Deserialize result: {res}");
        if (res) Debug.Log(val.Serialize().ToString());
        else Debug.Log(err);
    }

    [Test]
    public void TestVersioned()
    {
        var schema = new TestVersionedSchema();
        var json = "{\"int1\": 44.0, \"str1\": \"hello\"}";
        var res = schema.TryDeserialize(json, out var val, out var err);
        Assert.IsTrue(res);
        Debug.Log($"Deserialize result: {res}");
        if (res)
        {
            Debug.Log(val.Serialize().ToString());
            Debug.Log(val.As<IntSchemaValue>("int1").Value);
            Debug.Log(val.As<StringSchemaValue>("str1").Value);
        }
        else
        {
            Debug.Log(err);
        }
    }

    [Test]
    public void TestOldVersioned()
    {
        var schema = new TestVersionedSchema();
        var json = "{\"version\": \"0.1\", \"int1\": 44.0}";
        var res = schema.TryDeserialize(json, out var val, out var err);
        Assert.IsTrue(res);
        Debug.Log($"Deserialize result: {res}");
        if (res)
        {
            Debug.Log(val.Serialize().ToString());
            Debug.Log(val.As<IntSchemaValue>("int1").Value);
            Assert.AreEqual("default_value", val.As<StringSchemaValue>("str1").Value);
        }
        else
        {
            Debug.Log(err);
        }
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

public class TestVersionedSchema : VersionedSchema
{
    public TestVersionedSchema() : base(new Version2(), new()
            {
                new Version1(),
            })
    {
    }

    public class Version1 : OldSchemaVersion<Version2>
    {
        public Version1() : base("0.1") { }

        protected override (string key, Schema val)[] Values => new (string, Schema)[] { ("int1", new IntSchema()) };

        public override void Upgrade(ObjectSchemaValue val)
        {
            val.Values["str1"] = new StringSchemaValue("default_value");
        }
    }

    public class Version2 : SchemaVersion
    {
        public Version2() : base("0.2") { }

        protected override (string key, Schema val)[] Values => new (string, Schema)[] { ("int1", new IntSchema()), ("str1", new StringSchema()) };
    }
}