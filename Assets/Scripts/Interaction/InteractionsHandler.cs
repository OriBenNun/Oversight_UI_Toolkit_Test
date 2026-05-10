using System.Collections.Generic;
using DragDropValidation;
using Index;
using Model;
using UnityEngine;

namespace Interaction
{
    public enum DropMode
    {
        Before,
        Into,
        After
    }

    public class InteractionsHandler : MonoBehaviour
    {
        private IndexHandler _indexHandler;
        private DataHandler _dataHandler;
        private DragDropValidator _validator;
        private string _selectedNodeId;

        private float _searchDebounceTimer;
        private const float SearchDebounceSeconds = 0.3f;
        private string _pendingQuery;
        private bool _searchDirty;
        public bool IsSearchActive { get; private set; }

        public void Initialize(IndexHandler index, DataHandler data)
        {
            _indexHandler = index;
            _dataHandler = data;
            _validator = new DragDropValidator(_indexHandler.GetNodeById);
        }

        public DragDropValidator GetValidator() => _validator;

        // ── Search ─────────────────────────────────────────────────────────────

        public void OnSearchQueryChanged(string query)
        {
            _pendingQuery = query;
            _searchDebounceTimer = SearchDebounceSeconds;
            _searchDirty = true;
        }

        private void Update()
        {
            if (!_searchDirty) return;
            _searchDebounceTimer -= Time.deltaTime;
            if (_searchDebounceTimer <= 0f)
            {
                ApplySearch(_pendingQuery);
                _searchDirty = false;
            }
        }

        private void ApplySearch(string query)
        {
            var wasSearchActive = IsSearchActive;
            IsSearchActive = !string.IsNullOrWhiteSpace(query);
            _indexHandler.SetFilter(query);

            // When search clears, scroll back to the selected node so it stays in view.
            if (wasSearchActive && !IsSearchActive && _selectedNodeId != null)
                _indexHandler.RevealNode(_selectedNodeId);
        }

        // ── Keyboard navigation ────────────────────────────────────────────────

        // Returns the new flat list index after moving up (clamped at 0).
        public int MoveSelectionUp()
        {
            var flatList = _indexHandler.FlatList;
            if (flatList.Count == 0) return -1;
            var current = flatList.FindIndex(t => t.node.NodeId == _selectedNodeId);
            var next = Mathf.Max(0, current - 1);
            _selectedNodeId = flatList[next].node.NodeId;
            return next;
        }

        // Returns the new flat list index after moving down (clamped at end).
        public int MoveSelectionDown()
        {
            var flatList = _indexHandler.FlatList;
            if (flatList.Count == 0) return -1;
            var current = flatList.FindIndex(t => t.node.NodeId == _selectedNodeId);
            var next = Mathf.Min(flatList.Count - 1, current + 1);
            _selectedNodeId = flatList[next].node.NodeId;
            return next;
        }

        public void ToggleExpandSelected()
        {
            if (_selectedNodeId != null) ToggleExpand(_selectedNodeId);
        }

        // Returns the new flat list index to apply to ListView, or -1 if no index change is needed.
        public int HandleKeyDown(KeyCode keyCode)
        {
            switch (keyCode)
            {
                case KeyCode.UpArrow: return MoveSelectionUp();
                case KeyCode.DownArrow: return MoveSelectionDown();
                case KeyCode.Return:
                case KeyCode.Space:
                    if (!IsSearchActive) ToggleExpandSelected();
                    return -1;
                default: return -1;
            }
        }

        // ── Expand / Visibility ────────────────────────────────────────────────

        public void ExpandAll()
        {
            foreach (var root in _dataHandler.Roots)
                SetExpandedRecursive(root, true);
            _indexHandler.NotifyRebuildNeeded();
        }

        public void CollapseAll()
        {
            foreach (var root in _dataHandler.Roots)
                SetExpandedRecursive(root, false);
            _indexHandler.NotifyRebuildNeeded();
        }

        private static void SetExpandedRecursive(TreeNode node, bool expanded)
        {
            if (node.IsGroup) node.SetExpanded(expanded);
            foreach (var child in node.Children)
                SetExpandedRecursive(child, expanded);
        }

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

        // mode: Before/After = sibling; Into = first child of hovered group (works on collapsed groups too)
        public bool ExecuteDrop(string draggedId, string hoveredId, DropMode mode)
        {
            var dragged = _indexHandler.GetNodeById(draggedId);
            var hovered = _indexHandler.GetNodeById(hoveredId);
            if (dragged == null || hovered == null || draggedId == hoveredId) return false;

            string newParentId;
            int insertIndex;

            if (mode == DropMode.Into && hovered.IsGroup)
            {
                // Insert as first child of hovered group regardless of expanded state
                newParentId = hovered.NodeId;
                insertIndex = 0;
            }
            else
            {
                // Insert as a sibling before or after the hovered node
                newParentId = hovered.ParentId; // null means root level
                var siblings = GetSiblingList(newParentId);
                insertIndex = IndexInList(siblings, hovered);
                if (mode == DropMode.After) insertIndex++;

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