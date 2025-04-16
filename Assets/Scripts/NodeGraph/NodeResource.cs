using UnityEngine;

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
    }
}
