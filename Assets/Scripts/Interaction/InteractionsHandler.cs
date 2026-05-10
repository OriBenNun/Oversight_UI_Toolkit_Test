using System.Collections.Generic;
using DragDropValidation;
using Index;
using Model;
using UnityEngine;

namespace Interaction
{
    public class InteractionsHandler : MonoBehaviour
    {
        private IndexHandler _indexHandler;
        private DataHandler _dataHandler;
        private DragDropValidator _validator;
        private string _selectedNodeId;

        public void Initialize(IndexHandler index, DataHandler data)
        {
            _indexHandler = index;
            _dataHandler = data;
            _validator = new DragDropValidator(_indexHandler.GetNodeById);
        }

        public DragDropValidator GetValidator() => _validator;

        public void ToggleExpand(string nodeId)
        {
            var node = _indexHandler.GetNodeById(nodeId);
            if (node == null) return;
            node.SetExpanded(!node.IsExpanded);
            _indexHandler.NotifyRebuildNeeded();
        }

        public void ToggleVisibility(string nodeId)
        {
            var node = _indexHandler.GetNodeById(nodeId);
            if (node == null) return;

            if (node.IsGroup)
            {
                var setVisible = node.ComputeVisibilityState() != VisibilityState.Visible;
                SetVisibilityRecursive(node, setVisible);
            }
            else
                node.SetVisible(!node.IsVisible);

            _indexHandler.NotifyRebuildNeeded();
        }

        public void SetSelection(string nodeId) => _selectedNodeId = nodeId;
        public string GetSelection() => _selectedNodeId;

        // insertBefore: true = insert before hoveredId, false = insert after (or as first child if hovered is an expanded group)
        public bool ExecuteDrop(string draggedId, string hoveredId, bool insertBefore)
        {
            var dragged = _indexHandler.GetNodeById(draggedId);
            var hovered = _indexHandler.GetNodeById(hoveredId);
            if (dragged == null || hovered == null || draggedId == hoveredId) return false;

            string newParentId;
            int insertIndex;

            if (!insertBefore && hovered.IsGroup && hovered.IsExpanded)
            {
                // Insert as first child of the hovered expanded group
                newParentId = hovered.NodeId;
                insertIndex = 0;
            }
            else
            {
                // Insert as a sibling before or after the hovered node
                newParentId = hovered.ParentId; // null means root level
                var siblings = GetSiblingList(newParentId);
                insertIndex = IndexInList(siblings, hovered);
                if (!insertBefore) insertIndex++;

                // After dragged is removed from the same parent, all subsequent sibling indices shift down by one
                if (dragged.ParentId == newParentId)
                {
                    var draggedIdx = IndexInList(siblings, dragged);
                    if (draggedIdx >= 0 && draggedIdx < insertIndex) insertIndex--;
                }
            }

            if (!_validator.IsValidDrop(draggedId, newParentId)) return false;

            var oldParent = _indexHandler.GetNodeById(dragged.ParentId);
            var newParent = _indexHandler.GetNodeById(newParentId);
            _dataHandler.MoveNode(dragged, oldParent, newParent, insertIndex);
            return true;
        }

        private List<TreeNode> GetSiblingList(string parentId)
        {
            if (parentId == null) return _dataHandler.Roots;
            var parent = _indexHandler.GetNodeById(parentId);
            return parent?.Children ?? _dataHandler.Roots;
        }

        private static int IndexInList(List<TreeNode> list, TreeNode node)
        {
            for (var i = 0; i < list.Count; i++)
                if (list[i] == node)
                    return i;
            return -1;
        }

        private void SetVisibilityRecursive(TreeNode node, bool visible)
        {
            node.SetVisible(visible);
            foreach (var child in node.Children)
                SetVisibilityRecursive(child, visible);
        }
    }
}