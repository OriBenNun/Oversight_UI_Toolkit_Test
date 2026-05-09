using System;
using Oversight.Index;
using Oversight.Model;

namespace Oversight.Logic
{
    public enum VisibilityState { Visible, Hidden, Mixed }

    public class TreeInteractionLogic
    {
        private readonly TreeIndex _index;
        private string _selectedNodeId;

        public event Action OnFlatListInvalidated;

        public TreeInteractionLogic(TreeIndex index)
        {
            _index = index;
        }

        public void ToggleExpand(string nodeId)
        {
            var node = _index.GetNodeById(nodeId);
            if (node == null) return;
            node.SetExpanded(!node.IsExpanded);
            OnFlatListInvalidated?.Invoke();
        }

        public void ToggleVisibility(string nodeId)
        {
            var node = _index.GetNodeById(nodeId);
            if (node == null) return;

            if (node.IsGroup)
            {
                bool setVisible = ComputeVisibilityState(node) != VisibilityState.Visible;
                SetVisibilityRecursive(node, setVisible);
            }
            else
                node.SetVisible(!node.IsVisible);

            OnFlatListInvalidated?.Invoke();
        }

        public VisibilityState ComputeVisibilityState(TreeNode node)
        {
            if (!node.IsGroup)
                return node.IsVisible ? VisibilityState.Visible : VisibilityState.Hidden;

            bool anyVisible = false, anyHidden = false;
            CollectLeafStates(node, ref anyVisible, ref anyHidden);

            if (anyVisible && anyHidden) return VisibilityState.Mixed;
            if (anyHidden) return VisibilityState.Hidden;
            return VisibilityState.Visible;
        }

        private void CollectLeafStates(TreeNode node, ref bool anyVisible, ref bool anyHidden)
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

        public void SetSelection(string nodeId)
        {
            _selectedNodeId = nodeId;
        }

        public string GetSelection() => _selectedNodeId;

        private void SetVisibilityRecursive(TreeNode node, bool visible)
        {
            node.SetVisible(visible);
            foreach (var child in node.Children)
                SetVisibilityRecursive(child, visible);
        }
    }
}
