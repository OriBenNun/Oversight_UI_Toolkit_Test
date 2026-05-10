using System;
using System.Collections.Generic;
using UnityEngine;
using Oversight.Index;
using Oversight.Model;

namespace Oversight.UI
{
    public class IndexHandler : MonoBehaviour
    {
        private DataHandler _dataHandler;
        private TreeIndex _index;

        public event Action OnFlatListInvalidated;

        public void Initialize(DataHandler data)
        {
            _dataHandler = data;
            _index = new TreeIndex();
            _index.Build(_dataHandler.MutableRoots);
            _dataHandler.OnDataMutated += OnRebuildFull;
        }

        public TreeNode GetNodeById(string id) => _index.GetNodeById(id);

        public void RevealNode(string id) => _index.RevealNode(id);

        public void RebuildAndNotify()
        {
            OnFlatListInvalidated?.Invoke();
        }

        public List<(TreeNode node, int depth, VisibilityState visState)> BuildFlatList()
        {
            var raw = _index.BuildFlatList();
            var result = new List<(TreeNode, int, VisibilityState)>(raw.Count);
            foreach (var (node, depth) in raw)
                result.Add((node, depth, node.ComputeVisibilityState()));
            return result;
        }

        public List<(TreeNode node, int depth, VisibilityState visState)> FilterNodes(string query)
        {
            var raw = _index.FilterNodes(query);
            var result = new List<(TreeNode, int, VisibilityState)>(raw.Count);
            foreach (var (node, depth) in raw)
                result.Add((node, depth, node.ComputeVisibilityState()));
            return result;
        }

        private void OnRebuildFull()
        {
            _index.Build(_dataHandler.MutableRoots);
            OnFlatListInvalidated?.Invoke();
        }

        private void OnDestroy()
        {
            if (_dataHandler != null)
                _dataHandler.OnDataMutated -= OnRebuildFull;
        }
    }
}
