using System.Collections.Generic;
using Oversight.Model;

namespace Oversight.Index
{
    public class TreeIndex
    {
        private Dictionary<string, TreeNode> _idMap = new();
        private List<TreeNode> _roots = new();

        public void Build(List<TreeNode> roots)
        {
            _idMap.Clear();
            _roots = roots;
            foreach (var root in roots)
                RegisterNode(root);
        }

        public TreeNode GetNodeById(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            _idMap.TryGetValue(id, out var node);
            return node;
        }

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
        }

        public List<(TreeNode node, int depth)> BuildFlatList()
        {
            var result = new List<(TreeNode, int)>();
            foreach (var root in _roots)
                AppendShown(root, 0, result);
            return result;
        }

        public List<(TreeNode node, int depth)> FilterNodes(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return BuildFlatList();

            var q = query.ToLowerInvariant();

            // Collect matching node IDs
            var matchIds = new HashSet<string>();
            foreach (var kv in _idMap)
            {
                if (kv.Value.DisplayName.ToLowerInvariant().Contains(q))
                    matchIds.Add(kv.Key);
            }

            // Expand to include all ancestors
            var included = new HashSet<string>(matchIds);
            foreach (var id in matchIds)
            {
                var node = GetNodeById(id);
                var ancestor = GetNodeById(node?.ParentId);
                while (ancestor != null)
                {
                    included.Add(ancestor.NodeId);
                    ancestor = GetNodeById(ancestor.ParentId);
                }
            }

            // DFS to produce ordered result (only included, skip non-included subtrees)
            var result = new List<(TreeNode, int)>();
            foreach (var root in _roots)
                AppendFiltered(root, 0, included, result);

            return result;
        }

        private void RegisterNode(TreeNode node)
        {
            _idMap[node.NodeId] = node;
            foreach (var child in node.Children)
                RegisterNode(child);
        }

        private void AppendShown(TreeNode node, int depth, List<(TreeNode, int)> result)
        {
            result.Add((node, depth));
            if (node.IsExpanded)
            {
                foreach (var child in node.Children)
                    AppendShown(child, depth + 1, result);
            }
        }

        private void AppendFiltered(TreeNode node, int depth, HashSet<string> included, List<(TreeNode, int)> result)
        {
            if (!included.Contains(node.NodeId)) return;
            result.Add((node, depth));
            foreach (var child in node.Children)
                AppendFiltered(child, depth + 1, included, result);
        }

    }
}
