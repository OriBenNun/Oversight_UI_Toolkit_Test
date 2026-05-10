using System;
using Model;

namespace Data
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
