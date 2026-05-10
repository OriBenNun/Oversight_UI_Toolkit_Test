using System.Collections.Generic;

namespace Model
{
    public enum VisibilityState { Visible, Hidden, Mixed }

    public class TreeNode
    {
        public string NodeId { get; }
        public string DisplayName { get; }
        public NodeType NodeType { get; }
        public LayerType LayerType { get; }
        public string ParentId { get; private set; }

        public bool IsExpanded { get; private set; }

        public bool IsVisible { get; private set; }

        public List<TreeNode> Children { get; }

        public bool IsGroup { get; }
        
        public TreeNode(string nodeId, string displayName, NodeType type,
                         string parentId, LayerType layerType)
        {
            NodeId = nodeId;
            DisplayName = displayName;
            NodeType = type;
            IsGroup = type == NodeType.Group;
            LayerType = layerType;
            ParentId = parentId;
            IsExpanded = false;
            IsVisible = true;
            Children = new List<TreeNode>();
        }
        
        public void SetExpanded(bool value) => IsExpanded = value;
        public void SetVisible(bool value)  => IsVisible = value;
        public void SetParent(string parentId) => ParentId = parentId;
        public void AddChild(TreeNode child, int index) => Children.Insert(index, child);
        public void RemoveChild(TreeNode child) => Children.Remove(child);

        public VisibilityState ComputeVisibilityState()
        {
            if (!IsGroup)
                return IsVisible ? VisibilityState.Visible : VisibilityState.Hidden;

            bool anyVisible = false, anyHidden = false;
            CollectLeafStates(this, ref anyVisible, ref anyHidden);

            if (anyVisible && anyHidden) return VisibilityState.Mixed;
            // If anyHidden is true, then all leaves are hidden, otherwise all leaves are visible
            return anyHidden ? VisibilityState.Hidden : VisibilityState.Visible;
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
