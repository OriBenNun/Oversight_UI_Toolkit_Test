using System;
using System.Collections.Generic;
using Data;
using UnityEngine;

namespace Model
{
    public class DataHandler : MonoBehaviour
    {
        [SerializeField] private TreeDataAsset _treeDataAsset;
        
        private List<TreeNode> _roots;

        public List<TreeNode> Roots => _roots;

        public event Action OnDataMutated;

        public void Initialize()
        {
            _roots = LoadTreeData();
        }
        
        private List<TreeNode> LoadTreeData()
        {
            if (_treeDataAsset?.Nodes?.Length > 0)
                return ConstructTreeFromData(_treeDataAsset.Nodes);

            Debug.LogError("[Oversight] TreeDataAsset not assigned. Drag TreeData.asset onto the DataHandler component.");
            return new List<TreeNode>();
        }
        
        private static List<TreeNode> ConstructTreeFromData(NodeData[] nodes)
        {
            var map = new Dictionary<string, TreeNode>(nodes.Length);
            foreach (var d in nodes)
            {
                var pid = string.IsNullOrEmpty(d.ParentId) ? null : d.ParentId;
                map[d.Id] = new TreeNode(d.Id, d.DisplayName, d.NodeType, pid, d.LayerType);
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

        public void MoveNode(TreeNode dragged, TreeNode oldParent, TreeNode newParent, int insertIndex)
        {
            if (oldParent == null)
                _roots.Remove(dragged);
            else
                oldParent.RemoveChild(dragged);

            dragged.SetParent(newParent?.NodeId);

            var clampedIndex = Math.Clamp(insertIndex, 0, newParent != null ? newParent.Children.Count : _roots.Count);
            if (newParent == null)
                _roots.Insert(clampedIndex, dragged);
            else
                newParent.AddChild(dragged, clampedIndex);

            OnDataMutated?.Invoke();
        }
    }
}
