using JsonPattern;
using NUnit.Framework;

public class StringSchemaTest
{
    [Test]
    public void TestDeserializeValid()
    {
        var schema = new StringSchema();
        string[] testCases = { "", "1", " ", "\\\\", "\\n", "!§$%&/()=?²³{[]}}\\`´#'+*~@" };
        foreach (var test in testCases)
        {
            var json = new StringValue(test);
            Assert.IsTrue(schema.TryDeserialize(json, out var val, out _), $"Should be true for '{test}'");
            Assert.IsInstanceOf<StringSchemaValue>(val);
            Assert.AreEqual(test, ((StringSchemaValue)val).Value);
        }
    }

    [Test]
    public void TestDeserializeInvalidJson()
    {
        var schema = new StringSchema();
        JsonValue[] testCases = { new NumberValue(1), new ArrayValue(new()), new NullValue(), new BoolValue(true), new ObjectValue(new()) };
        foreach (var test in testCases)
        {
            Assert.IsFalse(schema.TryDeserialize(test, out _, out var err), $"Should be false for '{test}'");
            Assert.IsNotEmpty(err);
        }
    }
}