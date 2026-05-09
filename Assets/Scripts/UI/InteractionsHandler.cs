using System;
using System.Collections.Generic;
using UnityEngine;
using Oversight.Logic;
using Oversight.Model;

namespace Oversight.UI
{
    public class InteractionsHandler : MonoBehaviour
    {
        private IndexHandler _indexHandler;
        private DataHandler _dataHandler;
        private TreeInteractionLogic _logic;
        private DragDropValidator _validator;

        public event Action OnFlatListInvalidated;

        public void Initialize(IndexHandler index, DataHandler data)
        {
            _indexHandler = index;
            _dataHandler  = data;
            _logic     = new TreeInteractionLogic(_indexHandler.Index);
            _validator = new DragDropValidator(_indexHandler.Index, _dataHandler.MutableRoots);
            _logic.OnFlatListInvalidated        += () => OnFlatListInvalidated?.Invoke();
            _indexHandler.OnFlatListInvalidated += () => OnFlatListInvalidated?.Invoke();
        }

        public List<(TreeNode node, int depth, VisibilityState visState)> BuildFlatList()
            => _indexHandler.BuildFlatList();

        public List<(TreeNode node, int depth, VisibilityState visState)> FilterNodes(string query)
            => _indexHandler.FilterNodes(query);

        public void ToggleExpand(string nodeId)     => _logic.ToggleExpand(nodeId);
        public void ToggleVisibility(string nodeId) => _logic.ToggleVisibility(nodeId);
        public void SetSelection(string nodeId)     => _logic.SetSelection(nodeId);
        public string GetSelection()                => _logic.GetSelection();

        public bool IsValidDrop(string draggedId, string targetId)
            => _validator.IsValidDrop(draggedId, targetId);

        public void ExecuteDrop(string draggedId, string targetId, int insertIndex)
        {
            _validator.ExecuteDrop(draggedId, targetId, insertIndex);
            _dataHandler.NotifyMutated();
        }
    }
}
