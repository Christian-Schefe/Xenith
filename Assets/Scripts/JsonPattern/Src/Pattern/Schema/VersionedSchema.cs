using System.Collections.Generic;
using System.Linq;

namespace JsonPattern
{
    public class VersionedSchema : JsonSchema<ObjectSchemaValue, ObjectValue>
    {
        private readonly static StringSchema versionSchema = new();
        private readonly Dictionary<string, ISchemaVersion> versions;
        private readonly ISchemaVersion currentVersion;

        public VersionedSchema(ISchemaVersion currentVersion, List<IOldSchemaVersion> oldVersions)
        {
            versions = oldVersions.ToDictionary(v => v.Version, v => (ISchemaVersion)v);
            versions.Add(currentVersion.Version, currentVersion);
            this.currentVersion = currentVersion;
            foreach (var oldVersion in oldVersions)
            {
                ValidateVersionTree(oldVersion.Version, currentVersion.Version);
            }
        }

        private void ValidateVersionTree(string fromVersion, string toVersion)
        {
            var current = fromVersion;
            var visited = new HashSet<string> { current };
            while (current != toVersion)
            {
                if (!versions.TryGetValue(current, out var currentVersion) || currentVersion is not IOldSchemaVersion oldVersion)
                {
                    throw new System.Exception($"Invalid version transition from {fromVersion} to {toVersion}. Invalid version {current}.");
                }
                current = oldVersion.CanUpgradeTo;
                if (!visited.Add(current))
                {
                    throw new System.Exception($"Invalid version transition from {fromVersion} to {toVersion}. Detected a cycle in the version upgrade path at version {oldVersion.Version} to {current}.");
                }
            }
        }

        internal override void DoDeserialization(ObjectValue json, DeserializationContext ctx)
        {
            string version = currentVersion.Version;
            if (json.values.TryGetValue("version", out var versionValue))
            {
                versionSchema.DoDeserialization(versionValue, ctx);
                if (ctx.IsError) return;
                version = ((StringSchemaValue)ctx.Pop()).Value;
            }

            if (!versions.TryGetValue(version, out var schemaVersion))
            {
                ctx.Error(json, $"Unsupported schema version: {version}");
                return;
            }
            schemaVersion.Schema.DoDeserialization(json, ctx);
            if (ctx.IsError) return;
            if (schemaVersion is not IOldSchemaVersion) return;

            var val = (ObjectSchemaValue)ctx.Pop();
            while (schemaVersion is IOldSchemaVersion oldVersion)
            {
                oldVersion.Upgrade(val);
                schemaVersion = versions[oldVersion.CanUpgradeTo];
            }
            ctx.Push(val);
        }
    }

    public interface ISchemaVersion
    {
        string Version { get; }
        ObjectSchema Schema { get; }
    }

    public interface IOldSchemaVersion : ISchemaVersion
    {
        string CanUpgradeTo { get; }

        void Upgrade(ObjectSchemaValue val);
    }

    public abstract class OldSchemaVersion<T> : SchemaVersion, IOldSchemaVersion where T : ISchemaVersion, new()
    {
        private readonly string canUpgradeTo = new T().Version;

        public OldSchemaVersion(string version) : base(version) { }

        public string CanUpgradeTo => canUpgradeTo;

        public abstract void Upgrade(ObjectSchemaValue val);
    }

    public abstract class SchemaVersion : ClassSchema, ISchemaVersion
    {
        private readonly string version;

        public string Version => version;

        public ObjectSchema Schema => this;

        public SchemaVersion(string version)
        {
            this.version = version;
        }
    }
}