using System.Collections.Generic;

namespace Oversight.Model
{
    public enum VisibilityState { Visible, Hidden, Mixed }

    public class TreeNode
    {
        private readonly List<TreeNode> _children;

        public string NodeId { get; }
        public string DisplayName { get; }
        public NodeType NodeType { get; }
        public LayerType LayerType { get; }
        public string ParentId { get; private set; }

        public bool IsExpanded { get; private set; }

        public bool IsVisible { get; private set; }

        public IReadOnlyList<TreeNode> Children => _children;

        private TreeNode(string nodeId, string displayName, NodeType type,
                         string parentId, LayerType layerType)
        {
            NodeId = nodeId;
            DisplayName = displayName;
            NodeType = type;
            LayerType = layerType;
            ParentId = parentId;
            IsExpanded = false;
            IsVisible = true;
            _children = new List<TreeNode>();
        }

        public static TreeNode PopulateNewNode(string nodeId, string displayName, NodeType type, string parentId,
            LayerType layerType) =>
            new(nodeId, displayName, type, parentId, layerType);

        public bool IsGroup => NodeType == NodeType.Group;
        
        public void SetExpanded(bool value) => IsExpanded = value;
        public void SetVisible(bool value)  => IsVisible = value;
        public void SetParent(string parentId) => ParentId = parentId;
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
