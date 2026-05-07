using System;
using System.Collections.Generic;

namespace Oversight.Model
{
    [Serializable]
    public class TreeNode
    {
        public string NodeId;
        public string ParentId;
        public string DisplayName;
        public NodeType NodeType;
        public bool IsExpanded;
        public bool IsVisible;
        public List<TreeNode> Children;

        public TreeNode(string displayName, NodeType type, string parentId = null)
        {
            NodeId = Guid.NewGuid().ToString();
            DisplayName = displayName;
            NodeType = type;
            ParentId = parentId;
            IsExpanded = false;
            IsVisible = true;
            Children = new List<TreeNode>();
        }

        public bool IsGroup => NodeType == NodeType.Group;
        public bool IsLeaf  => NodeType == NodeType.Item;
    }
}
