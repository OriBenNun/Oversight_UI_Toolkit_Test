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
            _validator = new DragDropValidator(_indexHandler.GetNodeById, () => _dataHandler.Roots);
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
        
        public bool ExecuteDrop(string draggedId, string targetId, int insertIndex)
        {
            if (!_validator.IsValidDrop(draggedId, targetId)) return false;
            var dragged = _indexHandler.GetNodeById(draggedId);
            var newParent = _indexHandler.GetNodeById(targetId);
            var oldParent = _indexHandler.GetNodeById(dragged.ParentId);
            _dataHandler.MoveNode(dragged, oldParent, newParent, insertIndex);
            return true;
        }

        private void SetVisibilityRecursive(TreeNode node, bool visible)
        {
            node.SetVisible(visible);
            foreach (var child in node.Children)
                SetVisibilityRecursive(child, visible);
        }

    }
}