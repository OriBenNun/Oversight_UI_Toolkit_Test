using System;
using Oversight.Index;
using Oversight.Model;

namespace Oversight.Logic
{
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
                SetVisibilityRecursive(node, !node.IsVisible);
            else
                node.SetVisible(!node.IsVisible);

            OnFlatListInvalidated?.Invoke();
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
