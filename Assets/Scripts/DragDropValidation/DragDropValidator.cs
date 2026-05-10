using System;
using System.Collections.Generic;
using Model;

namespace DragDropValidation
{
    public class DragDropValidator
    {
        private readonly Func<string, TreeNode> _getNode;
        private readonly Func<List<TreeNode>> _getRoots;

        public DragDropValidator(Func<string, TreeNode> getNode, Func<List<TreeNode>> getRoots)
        {
            _getNode   = getNode;
            _getRoots  = getRoots;
        }

        public bool IsValidDrop(string draggedId, string targetId)
        {
            if (draggedId == targetId) return false;
            if (IsDescendant(draggedId, targetId)) return false;

            var dragged = _getNode(draggedId);
            var target  = _getNode(targetId);
            if (dragged == null || target == null) return false;

            if (dragged.ParentId == target.ParentId)
            {
                var siblings = GetSiblingList(dragged.ParentId);
                int di = IndexIn(siblings, dragged);
                int ti = IndexIn(siblings, target);
                if (Math.Abs(di - ti) == 1) return false;
            }

            return true;
        }

        public bool IsDescendant(string ancestorId, string candidateId)
        {
            var current = _getNode(candidateId);
            if (current == null) return false;

            while (current.ParentId != null)
            {
                current = _getNode(current.ParentId);
                if (current == null) break;
                if (current.NodeId == ancestorId) return true;
            }
            return false;
        }

        private IReadOnlyList<TreeNode> GetSiblingList(string parentId)
        {
            if (parentId == null) return _getRoots();
            var parent = _getNode(parentId);
            return parent?.Children ?? _getRoots();
        }

        private static int IndexIn(IReadOnlyList<TreeNode> list, TreeNode node)
        {
            for (int i = 0; i < list.Count; i++)
                if (list[i] == node) return i;
            return -1;
        }
    }
}
