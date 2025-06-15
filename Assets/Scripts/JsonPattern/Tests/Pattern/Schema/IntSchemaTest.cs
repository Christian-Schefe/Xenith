using JsonPattern;
using NUnit.Framework;

public class IntSchemaTest
{
    [Test]
    public void TestDeserializeValid()
    {
        var schema = new IntSchema();
        double[] testCases = { 0, 1, -1, 1234, -1234, int.MaxValue, int.MinValue };
        foreach (var test in testCases)
        {
            var json = new NumberValue(test);
            Assert.IsTrue(schema.TryDeserialize(json, out var val, out _), $"Should be true for '{test}'");
            Assert.IsInstanceOf<IntSchemaValue>(val);
            Assert.AreEqual(test, ((IntSchemaValue)val).Value);
        }
    }

    [Test]
    public void TestDeserializeInvalidDouble()
    {
        var schema = new IntSchema();
        double[] testCases = { 0.001, -0.001, 42.5, -42.5, double.Epsilon, -double.Epsilon };
        foreach (var test in testCases)
        {
            var json = new NumberValue(test);
            Assert.IsFalse(schema.TryDeserialize(json, out _, out var err), $"Should be false for '{test}'");
            Assert.IsNotEmpty(err);
        }
    }
    [Test]
    public void TestDeserializeInvalidJson()
    {
        var schema = new IntSchema();
        JsonValue[] testCases = { new StringValue("1"), new ArrayValue(new()), new NullValue(), new BoolValue(true), new ObjectValue(new()) };
        foreach (var test in testCases)
        {
            Assert.IsFalse(schema.TryDeserialize(test, out _, out var err), $"Should be false for '{test}'");
            Assert.IsNotEmpty(err);
        }
    }
}