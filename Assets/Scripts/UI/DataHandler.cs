using System.Collections.Generic;
using UnityEngine;
using Oversight.Data;
using Oversight.Model;

namespace Oversight.UI
{
    public class DataHandler : MonoBehaviour
    {
        [SerializeField] private TreeDataAsset _treeDataAsset;
        private List<TreeNode> _roots;

        public IReadOnlyList<TreeNode> Roots => _roots;
        public List<TreeNode> MutableRoots => _roots;

        public event System.Action OnDataMutated;
        public void NotifyMutated() => OnDataMutated?.Invoke();

        public void Initialize()
        {
            _roots = LoadOrGenerateTree();
        }

        private List<TreeNode> LoadOrGenerateTree()
        {
            if (_treeDataAsset?.Nodes?.Length > 0)
                return ReconstructTree(_treeDataAsset.Nodes);

            Debug.LogError("[Oversight] TreeDataAsset not assigned. Drag TreeData.asset onto the DataHandler component.");
            return new List<TreeNode>();
        }

        private static List<TreeNode> ReconstructTree(NodeData[] nodes)
        {
            var map = new Dictionary<string, TreeNode>(nodes.Length);
            foreach (var d in nodes)
            {
                string pid = string.IsNullOrEmpty(d.ParentId) ? null : d.ParentId;
                map[d.Id] = TreeNode.PopulateNewNode(d.Id, d.DisplayName, d.NodeType, pid, d.LayerType);
            }

            var roots = new List<TreeNode>();
            foreach (var d in nodes)
            {
                var node = map[d.Id];
                if (string.IsNullOrEmpty(d.ParentId))
                    roots.Add(node);
                else if (map.TryGetValue(d.ParentId, out var parent))
                    parent.AddChild(node, parent.Children.Count);
            }
            return roots;
        }
    }
}
