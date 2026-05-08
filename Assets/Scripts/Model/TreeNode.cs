using System;
using System.Collections.Generic;

namespace Oversight.Model
{
    public class TreeNode
    {
        private string _parentId;
        private bool _isExpanded;
        private bool _isVisible;
        private readonly List<TreeNode> _children;

        public string NodeId { get; }
        public string DisplayName { get; }
        public NodeType NodeType { get; }
        public string ParentId => _parentId;
        public bool IsExpanded => _isExpanded;
        public bool IsVisible => _isVisible;
        public IReadOnlyList<TreeNode> Children => _children;

        public TreeNode(string displayName, NodeType type, string parentId = null)
        {
            NodeId = Guid.NewGuid().ToString();
            DisplayName = displayName;
            NodeType = type;
            _parentId = parentId;
            _isExpanded = false;
            _isVisible = true;
            _children = new List<TreeNode>();
        }

        public bool IsGroup => NodeType == NodeType.Group;
        public bool IsLeaf  => NodeType == NodeType.Item;

        internal void SetExpanded(bool value) => _isExpanded = value;
        internal void SetVisible(bool value)  => _isVisible = value;
        internal void SetParent(string parentId) => _parentId = parentId;
        internal void AddChild(TreeNode child, int index) => _children.Insert(index, child);
        internal void RemoveChild(TreeNode child) => _children.Remove(child);
    }
}
