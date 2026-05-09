using System.Collections.Generic;

namespace Oversight.Model
{
    public enum VisibilityState { Visible, Hidden, Mixed }

    public class TreeNode
    {
        private string _parentId;
        private bool _isExpanded;
        private bool _isVisible;
        private readonly List<TreeNode> _children;

        public string NodeId { get; }
        public string DisplayName { get; }
        public NodeType NodeType { get; }
        public LayerType LayerType { get; }
        public string ParentId => _parentId;
        public bool IsExpanded => _isExpanded;
        public bool IsVisible => _isVisible;
        public IReadOnlyList<TreeNode> Children => _children;

        private TreeNode(string nodeId, string displayName, NodeType type,
                         string parentId, LayerType layerType)
        {
            NodeId = nodeId;
            DisplayName = displayName;
            NodeType = type;
            LayerType = layerType;
            _parentId = parentId;
            _isExpanded = false;
            _isVisible = true;
            _children = new List<TreeNode>();
        }

        public static TreeNode PopulateNewNode(string nodeId, string displayName, NodeType type, string parentId,
            LayerType layerType) =>
            new(nodeId, displayName, type, parentId, layerType);

        public bool IsGroup => NodeType == NodeType.Group;
        public bool IsLeaf  => NodeType == NodeType.Item;

        public void SetExpanded(bool value) => _isExpanded = value;
        public void SetVisible(bool value)  => _isVisible = value;
        public void SetParent(string parentId) => _parentId = parentId;
        public void AddChild(TreeNode child, int index) => _children.Insert(index, child);
        public void RemoveChild(TreeNode child) => _children.Remove(child);

        public VisibilityState ComputeVisibilityState()
        {
            if (!IsGroup)
                return IsVisible ? VisibilityState.Visible : VisibilityState.Hidden;

            bool anyVisible = false, anyHidden = false;
            CollectLeafStates(this, ref anyVisible, ref anyHidden);

            if (anyVisible && anyHidden) return VisibilityState.Mixed;
            if (anyHidden) return VisibilityState.Hidden;
            return VisibilityState.Visible;
        }

        private static void CollectLeafStates(TreeNode node, ref bool anyVisible, ref bool anyHidden)
        {
            if (anyVisible && anyHidden) return;
            foreach (var child in node.Children)
            {
                if (child.IsGroup)
                    CollectLeafStates(child, ref anyVisible, ref anyHidden);
                else if (child.IsVisible)
                    anyVisible = true;
                else
                    anyHidden = true;
            }
        }
    }
}
