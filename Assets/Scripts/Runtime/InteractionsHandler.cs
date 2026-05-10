using System;
using System.Collections.Generic;
using Logic;
using Model;
using UnityEngine;

namespace Runtime
{
    public class InteractionsHandler : MonoBehaviour
    {
        private IndexHandler _indexHandler;
        private DataHandler _dataHandler;
        private DragDropValidator _validator;
        private string _selectedNodeId;

        public event Action OnFlatListInvalidated;

        public void Initialize(IndexHandler index, DataHandler data)
        {
            _indexHandler = index;
            _dataHandler  = data;
            _validator    = new DragDropValidator(_indexHandler.GetNodeById, _dataHandler.Roots);
            _indexHandler.OnFlatListInvalidated += () => OnFlatListInvalidated?.Invoke();
        }

        public List<(TreeNode node, int depth, VisibilityState visState)> BuildFlatList()
            => _indexHandler.BuildFlatList();

        public List<(TreeNode node, int depth, VisibilityState visState)> FilterNodes(string query)
            => _indexHandler.FilterNodes(query);

        public void ToggleExpand(string nodeId)
        {
            var node = _indexHandler.GetNodeById(nodeId);
            if (node == null) return;
            node.SetExpanded(!node.IsExpanded);
            _indexHandler.RebuildAndNotify();
        }

        public void ToggleVisibility(string nodeId)
        {
            var node = _indexHandler.GetNodeById(nodeId);
            if (node == null) return;

            if (node.IsGroup)
            {
                bool setVisible = node.ComputeVisibilityState() != VisibilityState.Visible;
                SetVisibilityRecursive(node, setVisible);
            }
            else
                node.SetVisible(!node.IsVisible);

            _indexHandler.RebuildAndNotify();
        }

        public void SetSelection(string nodeId) => _selectedNodeId = nodeId;
        public string GetSelection()            => _selectedNodeId;

        public bool IsValidDrop(string draggedId, string targetId)
            => _validator.IsValidDrop(draggedId, targetId);

        public void ExecuteDrop(string draggedId, string targetId, int insertIndex)
        {
            if (!IsValidDrop(draggedId, targetId)) return;
            var dragged   = _indexHandler.GetNodeById(draggedId);
            var newParent = _indexHandler.GetNodeById(targetId);
            if (dragged == null || newParent == null) return;
            var oldParent = _indexHandler.GetNodeById(dragged.ParentId);
            _dataHandler.MoveNode(dragged, oldParent, newParent, insertIndex);
        }

        private void SetVisibilityRecursive(TreeNode node, bool visible)
        {
            node.SetVisible(visible);
            foreach (var child in node.Children)
                SetVisibilityRecursive(child, visible);
        }
    }
}
