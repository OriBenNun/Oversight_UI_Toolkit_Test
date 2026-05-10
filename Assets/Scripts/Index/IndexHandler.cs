using System;
using System.Collections.Generic;
using Model;
using UnityEngine;

namespace Index
{
    public class IndexHandler : MonoBehaviour
    {
        private DataHandler _dataHandler;
        private readonly Dictionary<string, TreeNode> _idMap = new();
        private List<(TreeNode node, int depth, VisibilityState visState)> _flatList = new();
        private string _activeQuery;

        public event Action OnFlatListInvalidated;
        public event Action<string> OnRevealNode;

        public List<(TreeNode node, int depth, VisibilityState visState)> FlatList => _flatList;

        public void Initialize(DataHandler data)
        {
            _dataHandler = data;
            BuildIndexData();
            RebuildFlatList();
            _dataHandler.OnDataMutated += NotifyRebuildNeeded;
        }

        private void OnDestroy()
        {
            if (_dataHandler)
                _dataHandler.OnDataMutated -= NotifyRebuildNeeded;
        }

        // Index-based lookup (by string id)
        // We use a dictionary for a fast O(1) lookup.
        // We have to check if the id is null to avoid null reference exceptions.
        // If the id is null or wasn't found in the dictionary, we return null.
        // Otherwise, we return the node associated with the id.
        public TreeNode GetNodeById(string id)
        {
            if (id == null) return null;
            _idMap.TryGetValue(id, out var node);
            return node;
        }

        // Reveals a node and all its ancestors by setting their expanded state to true.
        // We first get the node by id, and if it's null, we return.
        // Then we get the parent node and set its expanded state to true.
        // We repeat this process until we reach the root node.
        // Finally, it rebuilds the flat list and scrolls the list to the node.
        public void RevealNode(string id)
        {
            var node = GetNodeById(id);
            if (node == null) return;

            var current = GetNodeById(node.ParentId);
            while (current != null)
            {
                current.SetExpanded(true);
                current = GetNodeById(current.ParentId);
            }

            NotifyRebuildNeeded();
            OnRevealNode?.Invoke(id);
        }

        public void SetFilter(string query)
        {
            _activeQuery = query;
            NotifyRebuildNeeded();
        }

        public void NotifyRebuildNeeded()
        {
            RebuildFlatList();
            OnFlatListInvalidated?.Invoke();
        }

        private void RebuildFlatList()
        {
            _flatList = string.IsNullOrWhiteSpace(_activeQuery)
                ? BuildFlatList()
                : FilterNodes(_activeQuery);
        }

        private List<(TreeNode node, int depth, VisibilityState visState)> BuildFlatList()
        {
            var result = new List<(TreeNode, int, VisibilityState)>();
            foreach (var root in _dataHandler.Roots)
                AppendShown(root, 0, result);
            return result;
        }

        // Simple search/filter by name (using Contains)
        // We use HashSet for fast lookups and a two-pass approach — first pass builds all included in unordered way (including ancestors). Second pass outputs in correct DFS
        // order, when we already know exactly what to keep.
        private List<(TreeNode node, int depth, VisibilityState visState)> FilterNodes(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return BuildFlatList();

            var q = query.ToLowerInvariant();

            var matchIds = new HashSet<string>();
            foreach (var kv in _idMap)
            {
                if (kv.Value.DisplayName.ToLowerInvariant().Contains(q))
                    matchIds.Add(kv.Key);
            }

            var included = new HashSet<string>(matchIds);
            foreach (var id in matchIds)
            {
                var ancestor = GetNodeById(GetNodeById(id)?.ParentId);
                while (ancestor != null)
                {
                    included.Add(ancestor.NodeId);
                    ancestor = GetNodeById(ancestor.ParentId);
                }
            }

            var filtered = new List<(TreeNode, int, VisibilityState)>();
            foreach (var root in _dataHandler.Roots)
                AppendFiltered(root, 0, included, filtered);
            return filtered;
        }

        private void BuildIndexData()
        {
            _idMap.Clear();
            foreach (var root in _dataHandler.Roots)
                RegisterNode(root);
        }

        private void RegisterNode(TreeNode node)
        {
            _idMap[node.NodeId] = node;
            foreach (var child in node.Children)
                RegisterNode(child);
        }

        private void AppendShown(TreeNode node, int depth, List<(TreeNode, int, VisibilityState)> result)
        {
            result.Add((node, depth, node.ComputeVisibilityState()));
            if (node.IsExpanded)
            {
                foreach (var child in node.Children)
                    AppendShown(child, depth + 1, result);
            }
        }

        private void AppendFiltered(TreeNode node, int depth, HashSet<string> included,
            List<(TreeNode, int, VisibilityState)> result)
        {
            if (!included.Contains(node.NodeId)) return;
            result.Add((node, depth, node.ComputeVisibilityState()));
            foreach (var child in node.Children)
                AppendFiltered(child, depth + 1, included, result);
        }
    }
}