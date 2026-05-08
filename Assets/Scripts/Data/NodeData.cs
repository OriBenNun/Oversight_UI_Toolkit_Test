using System;
using Oversight.Model;

namespace Oversight.Data
{
    [Serializable]
    public struct NodeData
    {
        public string    Id;
        public string    ParentId;
        public string    DisplayName;
        public NodeType  NodeType;
        public LayerType LayerType;
    }
}
