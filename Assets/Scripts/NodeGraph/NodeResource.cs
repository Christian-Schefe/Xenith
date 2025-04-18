namespace NodeGraph
{
    public struct NodeResource
    {
        public string displayName;
        public string id;
        public bool builtIn;

        public NodeResource(string displayName, string id, bool builtIn)
        {
            this.displayName = displayName;
            this.id = id;
            this.builtIn = builtIn;
        }

        public override readonly string ToString()
        {
            return $"{displayName} ({id})";
        }

        public override readonly bool Equals(object obj)
        {
            if (obj is NodeResource other)
            {
                return id == other.id;
            }
            return false;
        }

        public override readonly int GetHashCode()
        {
            return id.GetHashCode();
        }

        public static bool operator ==(NodeResource a, NodeResource b)
        {
            return a.Equals(b);
        }

        public static bool operator !=(NodeResource a, NodeResource b)
        {
            return !a.Equals(b);
        }
    }
}
